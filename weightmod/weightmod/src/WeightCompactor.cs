using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace weightmod.src
{
    // Compacts WEIGHTS_FOR_ITEMS / WEIGHTS_FOR_BLOCKS: finds groups of keys
    // that differ in exactly one segment (after splitting by '-') and
    // replaces such a group with a single WeightRule with '*' at that position.
    //
    // Example input:
    //   game:drystonefence-granite-nesw-free   240
    //   game:drystonefence-basalt-nesw-free    240
    //   game:drystonefence-slate-nesw-free     240
    //   ...
    // Output — one rule:
    //   pattern = "game:drystonefence-*-nesw-free", weight = 240, kind = "block"
    //
    // To avoid swallowing accidental 2-3-key collisions, the varying segment
    // must be a known value from Config.COMPACTOR_VOCABULARIES
    // (rock / wood / metal / direction / ...).
    internal static class WeightCompactor
    {
        private const int MinGroupSize = 3;
        private const int MaxPasses    = 3;
        private const float WeightEps  = 0.001f;

        // A compaction candidate: one pattern with a single '*' that covered
        // some number of keys sharing the same weight.
        private sealed class Candidate
        {
            public string Pattern;        // "game:drystonefence-*-nesw-free"
            public float  Weight;         // 240
            public List<string> Keys;     // all keys that fell into this group
        }

        public static void Compact(Config config, ICoreAPI api)
        {
            int rules   = 0;
            int removed = 0;

            var vocab = BuildVocabulary(config);

            var (r1, k1) = CompactDict(config.WEIGHTS_FOR_ITEMS,  "item",  config, vocab);
            var (r2, k2) = CompactDict(config.WEIGHTS_FOR_BLOCKS, "block", config, vocab);

            rules   = r1 + r2;
            removed = k1 + k2;

            api?.Logger.Notification(
                $"[weightmod] compactor: emitted {rules} rules, removed {removed} exact keys");
        }

        // Flattens all COMPACTOR_VOCABULARIES groups into one HashSet
        // so the "is segment known" check is a single Contains call.
        private static HashSet<string> BuildVocabulary(Config config)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (config.COMPACTOR_VOCABULARIES == null) return set;
            foreach (var kvp in config.COMPACTOR_VOCABULARIES)
            {
                if (kvp.Value == null) continue;
                foreach (var s in kvp.Value)
                {
                    if (!string.IsNullOrEmpty(s)) set.Add(s);
                }
            }
            return set;
        }

        // Main loop. Up to MaxPasses passes: collapsing one position
        // may open up the possibility to collapse another.
        private static (int rulesAdded, int keysRemoved) CompactDict(
            IDictionary<string, float> dict, string kind, Config config, HashSet<string> vocab)
        {
            int rulesAdded  = 0;
            int keysRemoved = 0;

            // Single HashSet of existing + emitted patterns — O(1) duplicate check
            // instead of a linear scan of WEIGHT_RULES on every emit.
            var emittedPatterns = new HashSet<string>(StringComparer.Ordinal);
            if (config.WEIGHT_RULES != null)
            {
                foreach (var r in config.WEIGHT_RULES)
                {
                    if (r?.Pattern != null) emittedPatterns.Add(r.Pattern);
                }
            }

            // Pre-purge: oracle re-adds entries that an earlier compactor run already
            // collapsed into a wildcard rule. Without this, every run cycles them back
            // because the existing rule causes new emit to be skipped on the duplicate
            // check, leaving the dict entries untouched. Sweep all wildcard rules of
            // this kind once and drop any dict key they cover with the matching weight.
            keysRemoved += PrePurgeExistingRules(dict, kind, config);

            // Existing exact rules (no '*') of this kind — used at emit time to refuse
            // a wildcard pattern that would semantically conflict with a hand-written
            // rule like "game:metalplate-copper: 178". The runtime already gives the
            // exact rule precedence (first match wins), but we don't want to write a
            // contradictory wildcard rule into the config.
            var exactRulesByKind = new List<(string pattern, float weight)>();
            if (config.WEIGHT_RULES != null)
            {
                foreach (var r in config.WEIGHT_RULES)
                {
                    if (r?.Pattern == null) continue;
                    if (r.Pattern.IndexOf('*') >= 0) continue;
                    if (!string.Equals(r.Kind, kind, StringComparison.OrdinalIgnoreCase)) continue;
                    exactRulesByKind.Add((r.Pattern, r.Weight));
                }
            }

            for (int pass = 0; pass < MaxPasses; pass++)
            {
                var (candidates, conflicts) = FindCandidates(dict, vocab);
                if (candidates.Count == 0) break;

                int emittedThisPass = 0;

                // Largest groups first — collapse the biggest wins before smaller ones.
                candidates.Sort((a, b) => b.Keys.Count.CompareTo(a.Keys.Count));

                // Reusable buffers for the per-emit wildcard scan so we don't allocate
                // a fresh List<string> for every candidate.
                var matchedSameWeight = new List<string>(32);

                foreach (var cand in candidates)
                {
                    // Some keys may have already been removed by an earlier emit this pass.
                    int aliveCount = CountAlive(cand, dict);
                    if (aliveCount < MinGroupSize) continue;

                    // Fast O(1) pre-filter — rejects most unsafe / duplicate patterns
                    // without touching WildcardUtil.
                    if (!IsPatternSafe(cand.Pattern, conflicts, emittedPatterns)) continue;

                    // Final safety + cleanup scan over the whole dict.
                    // '*' in WildcardUtil.Match spans hyphens, so a pattern like
                    // "game:stone-*" also matches "game:stone-granite-block" — those
                    // longer keys aren't in cand.Keys and the fast conflict-tracker
                    // can't see them. We need a wildcard scan to:
                    //   1) abort if any matched key has a different weight (correctness);
                    //   2) collect ALL matched keys to delete (cleanup of the tail).
                    matchedSameWeight.Clear();
                    bool conflict = false;
                    foreach (var kvp in dict)
                    {
                        if (!WildcardUtil.Match(cand.Pattern, kvp.Key)) continue;
                        if (Math.Abs(kvp.Value - cand.Weight) >= WeightEps)
                        {
                            conflict = true;
                            break;
                        }
                        matchedSameWeight.Add(kvp.Key);
                    }
                    if (conflict) continue;
                    if (matchedSameWeight.Count < MinGroupSize) continue;

                    // Conflict against existing exact rules of this kind.
                    // E.g. cand "game:metalplate-*" at 640 must not be emitted if
                    // "game:metalplate-copper: 178" already exists — they semantically
                    // contradict even though runtime order would make the exact rule win.
                    bool exactConflict = false;
                    foreach (var (rulePattern, ruleWeight) in exactRulesByKind)
                    {
                        if (Math.Abs(ruleWeight - cand.Weight) < WeightEps) continue;
                        if (WildcardUtil.Match(cand.Pattern, rulePattern))
                        {
                            exactConflict = true;
                            break;
                        }
                    }
                    if (exactConflict) continue;

                    config.WEIGHT_RULES.Add(new WeightRule
                    {
                        Pattern = cand.Pattern,
                        Weight  = cand.Weight,
                        Kind    = kind,
                    });
                    emittedPatterns.Add(cand.Pattern);
                    rulesAdded++;

                    foreach (var k in matchedSameWeight)
                    {
                        if (dict.Remove(k)) keysRemoved++;
                    }
                    emittedThisPass++;
                }

                if (emittedThisPass == 0) break;
            }

            var (pr, pk) = PrefixCollapse(dict, kind, config, emittedPatterns, exactRulesByKind);
            rulesAdded  += pr;
            keysRemoved += pk;

            return (rulesAdded, keysRemoved);
        }

        // Fallback collapse: after segment-wildcard passes, group remaining keys by
        // every possible prefix depth (1 segment up to parts.Length-1) and emit a
        // "prefix-*" rule for groups where every key shares the same weight and
        // count >= MinGroupSize. Longer prefixes are tried first so more specific
        // patterns win over broader ones. Handles ad-hoc namespaces where item
        // names don't follow a uniform schema (flour, fruit, clothes, etc.).
        private static (int rulesAdded, int keysRemoved) PrefixCollapse(
            IDictionary<string, float> dict, string kind, Config config,
            HashSet<string> emittedPatterns, List<(string pattern, float weight)> exactRulesByKind)
        {
            var byPrefix = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var sb = new StringBuilder(64);
            foreach (var kvp in dict)
            {
                if (!TrySplit(kvp.Key, out string domain, out string[] parts)) continue;
                for (int depth = 1; depth < parts.Length; depth++)
                {
                    sb.Clear();
                    sb.Append(domain).Append(':');
                    for (int j = 0; j < depth; j++)
                    {
                        if (j > 0) sb.Append('-');
                        sb.Append(parts[j]);
                    }
                    string prefix = sb.ToString();
                    if (!byPrefix.TryGetValue(prefix, out var list))
                        byPrefix[prefix] = list = new List<string>();
                    list.Add(kvp.Key);
                }
            }

            // Shorter prefix first: broader rules win when valid (all keys same weight).
            // More specific rules in WEIGHT_RULES already come earlier in the list and
            // will override via first-match-wins, so a broad fallback is safe.
            var sortedPrefixes = new List<string>(byPrefix.Keys);
            sortedPrefixes.Sort((a, b) =>
            {
                int ca = 0, cb = 0;
                for (int i = 0; i < a.Length; i++) if (a[i] == '-') ca++;
                for (int i = 0; i < b.Length; i++) if (b[i] == '-') cb++;
                return ca.CompareTo(cb);
            });

            int rulesAdded  = 0;
            int keysRemoved = 0;

            foreach (var prefix in sortedPrefixes)
            {
                var keys = byPrefix[prefix];

                int alive = 0;
                float w = 0;
                bool first = true;
                bool sameWeight = true;
                foreach (var k in keys)
                {
                    if (!dict.TryGetValue(k, out var kw)) continue;
                    if (first) { w = kw; first = false; alive++; continue; }
                    if (Math.Abs(kw - w) >= WeightEps) { sameWeight = false; break; }
                    alive++;
                }
                if (!sameWeight) continue;
                if (alive < MinGroupSize) continue;

                string pattern = prefix + "-*";
                if (emittedPatterns.Contains(pattern)) continue;

                bool exactConflict = false;
                foreach (var (rulePattern, ruleWeight) in exactRulesByKind)
                {
                    if (Math.Abs(ruleWeight - w) < WeightEps) continue;
                    if (WildcardUtil.Match(pattern, rulePattern)) { exactConflict = true; break; }
                }
                if (exactConflict) continue;

                config.WEIGHT_RULES.Add(new WeightRule
                {
                    Pattern = pattern,
                    Weight  = w,
                    Kind    = kind,
                });
                emittedPatterns.Add(pattern);
                rulesAdded++;

                foreach (var k in keys)
                {
                    if (dict.Remove(k)) keysRemoved++;
                }
            }

            return (rulesAdded, keysRemoved);
        }
        // Builds the candidate list: for every (key, segment-position-in-vocab)
        // produces a pattern with '*' at that position, grouped by (pattern, weight).
        // In parallel, over ALL positions (including segments not in the vocabulary),
        // records patternConflict — patterns that already collide on same-segment-count
        // keys. This is a fast pre-filter; final correctness is enforced by the
        // wildcard scan at emit time, which also catches cross-segment-count matches.
        private static (List<Candidate> candidates, HashSet<string> conflicts) FindCandidates(
            IDictionary<string, float> dict, HashSet<string> vocab)
        {
            // Pattern groups: dictionary key (pattern, weight) → original codes.
            var groups = new Dictionary<(string pattern, float weight), List<string>>();

            // Per-pattern conflict tracking.
            var patternFirstWeight = new Dictionary<string, float>(StringComparer.Ordinal);
            var patternConflict    = new HashSet<string>(StringComparer.Ordinal);

            // Reusable buffer instead of string.Join — avoids per-position
            // array and intermediate-string allocations.
            var sb = new StringBuilder(64);

            foreach (var kvp in dict)
            {
                string code   = kvp.Key;
                float  weight = kvp.Value;

                if (!TrySplit(code, out string domain, out string[] parts)) continue;

                for (int i = 0; i < parts.Length; i++)
                {
                    string original = parts[i];

                    sb.Clear();
                    sb.Append(domain).Append(':');
                    for (int j = 0; j < parts.Length; j++)
                    {
                        if (j > 0) sb.Append('-');
                        if (j == i) sb.Append('*');
                        else        sb.Append(parts[j]);
                    }
                    string pattern = sb.ToString();

                    // Conflict tracking — across all segments, even non-vocabulary ones,
                    // so the pattern can't accidentally match a key with a different weight.
                    if (!patternConflict.Contains(pattern))
                    {
                        if (patternFirstWeight.TryGetValue(pattern, out var prev))
                        {
                            if (Math.Abs(prev - weight) >= WeightEps)
                                patternConflict.Add(pattern);
                        }
                        else
                        {
                            patternFirstWeight[pattern] = weight;
                        }
                    }

                    // Only feed into candidates if the varying segment is in the vocabulary.
                    if (!vocab.Contains(original)) continue;

                    var groupKey = (pattern, weight);
                    if (!groups.TryGetValue(groupKey, out var list))
                        groups[groupKey] = list = new List<string>();
                    list.Add(code);
                }
            }

            // Flatten into a plain list, dropping groups that are too small.
            var result = new List<Candidate>(groups.Count);
            foreach (var (key, keys) in groups)
            {
                if (keys.Count < MinGroupSize) continue;
                result.Add(new Candidate
                {
                    Pattern = key.pattern,
                    Weight  = key.weight,
                    Keys    = keys,
                });
            }
            return (result, patternConflict);
        }

        // Splits "game:foo-bar-baz" → domain="game", parts=["foo","bar","baz"].
        private static bool TrySplit(string code, out string domain, out string[] parts)
        {
            int colon = code.IndexOf(':');
            if (colon < 0)
            {
                domain = null; parts = null; return false;
            }
            domain = code.Substring(0, colon);
            string path = code.Substring(colon + 1);
            parts = path.Split('-');
            return parts.Length >= 2;
        }

        // How many of the candidate's keys still exist in the dict with the expected weight.
        private static int CountAlive(Candidate cand, IDictionary<string, float> dict)
        {
            int alive = 0;
            foreach (var key in cand.Keys)
            {
                if (dict.TryGetValue(key, out var w) && Math.Abs(w - cand.Weight) < WeightEps)
                    alive++;
            }
            return alive;
        }

        // Drops dict entries that are already covered by an existing wildcard
        // WEIGHT_RULE of the given kind with the same weight. Entries with a
        // different weight are left alone — they intentionally override the rule.
        private static int PrePurgeExistingRules(
            IDictionary<string, float> dict, string kind, Config config)
        {
            if (config.WEIGHT_RULES == null || config.WEIGHT_RULES.Count == 0) return 0;

            // Collect (pattern, weight) for wildcard rules of this kind, and exact
            // patterns separately — exact rules win at runtime regardless of weight,
            // so any dict entry matching one is dead code.
            var wildcardRules = new List<(string pattern, float weight)>();
            var exactPatterns = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in config.WEIGHT_RULES)
            {
                if (r?.Pattern == null) continue;
                if (!string.Equals(r.Kind, kind, StringComparison.OrdinalIgnoreCase)) continue;
                if (r.Pattern.IndexOf('*') < 0) exactPatterns.Add(r.Pattern);
                else                            wildcardRules.Add((r.Pattern, r.Weight));
            }
            if (wildcardRules.Count == 0 && exactPatterns.Count == 0) return 0;

            var toRemove = new List<string>();
            foreach (var kvp in dict)
            {
                if (exactPatterns.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }
                foreach (var (pattern, weight) in wildcardRules)
                {
                    if (Math.Abs(kvp.Value - weight) >= WeightEps) continue;
                    if (WildcardUtil.Match(pattern, kvp.Key))
                    {
                        toRemove.Add(kvp.Key);
                        break;
                    }
                }
            }
            foreach (var k in toRemove) dict.Remove(k);
            return toRemove.Count;
        }

        // A rule is safe to emit when:
        //  1) the pattern won't match any key with a different weight — taken from
        //     the precomputed patternConflict set, O(1);
        //  2) no such rule has been emitted yet — O(1) via HashSet, no scan of WEIGHT_RULES.
        private static bool IsPatternSafe(string pattern, HashSet<string> conflicts,
            HashSet<string> emittedPatterns)
        {
            if (conflicts.Contains(pattern)) return false;
            if (emittedPatterns.Contains(pattern)) return false;
            return true;
        }
    }
}
