using System;

namespace weightmod.src
{
    public class WeightRule
    {
        public string Pattern { get; set; }
        public float Weight { get; set; }
        public string Kind { get; set; } = "item";
    }

    internal enum MatchKind { Exact, Prefix, Suffix, Contains }
    internal enum RuleKind { Item, Block, Bonus }

    internal class CompiledRule
    {
        public string Literal;
        public MatchKind MatchKind;
        public float Weight;
        public RuleKind Kind;

        public static CompiledRule TryParse(WeightRule r)
        {
            if (r == null || string.IsNullOrEmpty(r.Pattern)) return null;

            string p = r.Pattern;
            bool starStart = p.StartsWith("*");
            bool starEnd = p.EndsWith("*");
            string literal = p.Trim('*');

            MatchKind mk;
            if (starStart && starEnd) mk = MatchKind.Contains;
            else if (starStart)       mk = MatchKind.Suffix;
            else if (starEnd)         mk = MatchKind.Prefix;
            else                      mk = MatchKind.Exact;

            RuleKind rk = (r.Kind?.ToLowerInvariant()) switch
            {
                "block" => RuleKind.Block,
                "bonus" => RuleKind.Bonus,
                _ => RuleKind.Item,
            };

            return new CompiledRule { Literal = literal, MatchKind = mk, Weight = r.Weight, Kind = rk };
        }

        public bool Matches(string fullCode) => MatchKind switch
        {
            MatchKind.Exact    => fullCode.Equals(Literal, StringComparison.Ordinal),
            MatchKind.Prefix   => fullCode.StartsWith(Literal, StringComparison.Ordinal),
            MatchKind.Suffix   => fullCode.EndsWith(Literal, StringComparison.Ordinal),
            MatchKind.Contains => fullCode.Contains(Literal),
            _ => false,
        };
    }
}
