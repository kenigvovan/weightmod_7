using Vintagestory.API.Util;

namespace weightmod.src
{
    public class WeightRule
    {
        public string Pattern { get; set; }
        public float Weight { get; set; }
        public string Kind { get; set; } = "item";
    }

    internal enum RuleKind { Item, Block, Bonus }

    internal class CompiledRule
    {
        public string Pattern;
        public bool HasWildcard;
        public float Weight;
        public RuleKind Kind;

        public static CompiledRule TryParse(WeightRule r)
        {
            if (r == null || string.IsNullOrEmpty(r.Pattern)) return null;

            RuleKind rk = (r.Kind?.ToLowerInvariant()) switch
            {
                "block" => RuleKind.Block,
                "bonus" => RuleKind.Bonus,
                _ => RuleKind.Item,
            };

            return new CompiledRule
            {
                Pattern = r.Pattern,
                HasWildcard = r.Pattern.IndexOf('*') >= 0 || r.Pattern.Length > 0 && r.Pattern[0] == '@',
                Weight = r.Weight,
                Kind = rk,
            };
        }

        public bool Matches(string fullCode)
        {
            if (!HasWildcard) return fullCode.Equals(Pattern, System.StringComparison.Ordinal);
            return WildcardUtil.Match(Pattern, fullCode);
        }
    }
}
