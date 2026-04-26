using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace weightmod.src
{

    public class Config
    {
        public class ItemWeightInfo
        {
            public float? Weight { get; set; }
            public string Category { get; set; }
        }
        public float MAX_PLAYER_WEIGHT { get; set; } = 20000;
        public float MAX_ELK_WEIGHT { get; set; } = 40000;

        public float WEIGH_PLAYER_THRESHOLD { get; set; } = 0.7f;

        public float RATIO_MIN_MAX_WEIGHT_PLAYER_HEALTH { get; set; } = 0.6f;

        public float ACCUM_TIME_WEIGHT_CHECK { get; set; } = 2f;

        public bool PERCENT_MODIFIER_USED_ON_RAW_WEIGHT { get; set; } = false;

        public string CLASS_WEIGHT_BONUS { get; set; } = "commoner:0;hunter:500;malefactor:-500;clockmaker:-1000;blackguard:2000;tailor:-2000";

        public float HOW_OFTEN_RECHECK { get; set; } = 10f;

        public string INFO_COLOR_WEIGHT { get; set; } = "#F0C20B";
        public string INFO_COLOR_WEIGHT_BONUS { get; set; } = "#1F920E";

        public System.Collections.Generic.OrderedDictionary<string, float> WEIGHTS_FOR_ITEMS { get; set; } = new System.Collections.Generic.OrderedDictionary<string, float>();

        public Dictionary<string, float> BASE_WEIGHTS_BY_CATEGORY { get; set; } = new Dictionary<string, float>
        {
            {"tool", 500f},
            {"weapon", 800f},
            {"armor", 1000f},
            {"ore", 1000f},
            {"metal", 800f},
            {"wood", 400f},
            {"stone", 1000f},
            {"gem", 200f},
            {"woodblock", 400f},
            {"stoneblock", 1000f},
            {"metalblock", 1200f},
            {"glassblock", 600f},
            {"food", 100f},
            {"craftingmaterial", 200f},
            {"clothing", 300f},
            {"misc", 200f}
        };
        public Dictionary<string, float> MATERIAL_MULTIPLIERS { get; set; } = new Dictionary<string, float>
        {
            {"wood", 0.8f},
            {"stone", 1.2f},
            {"metal", 1.5f},
            {"cloth", 0.3f},
            {"leather", 0.5f},
            {"glass", 0.7f},
            {"gem", 1.0f}
        };

        public Dictionary<string, float> WEIGHTS_FOR_BLOCKS { get; set; } = new Dictionary<string, float>
        {

        };
        public Dictionary<string, ItemWeightInfo> WEIGHTINFOS_FOR_ITEMS { get; set; } = new Dictionary<string, ItemWeightInfo>
        {

        };
        public Dictionary<string, ItemWeightInfo> WEIGHTINFOS_FOR_BLOCKS { get; set; } = new Dictionary<string, ItemWeightInfo>
        {

        };

        public Dictionary<string, int> WEIGHTS_FOR_ENDS_WITH { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> WEIGHTS_BONUS_ITEMS { get; set; } = new Dictionary<string, int>();

        // Unified rule list with wildcard patterns.
        // pattern:  "game:ingot-copper" (exact), "game:ore-*" (prefix), "*-bountiful" (suffix), "*ore*" (contains)
        // kind:     "item" | "block" | "bonus"  (bonus writes weightbonusbags on items)
        // Rules are evaluated in order; first match wins. Processed BEFORE the legacy WEIGHTS_FOR_* dicts.
        public List<WeightRule> WEIGHT_RULES { get; set; } = new List<WeightRule>
        {
            // Ores (prefix on path after domain)
            new WeightRule { Pattern = "game:crystalizedore-poor-*",     Weight = 43,    Kind = "item" },
            new WeightRule { Pattern = "game:crystalizedore-medium-*",   Weight = 52,    Kind = "item" },
            new WeightRule { Pattern = "game:crystalizedore-rich-*",     Weight = 75,    Kind = "item" },
            new WeightRule { Pattern = "game:crystalizedore-bountiful-*", Weight = 91,   Kind = "item" },

            // Quartz/cassiterite/hematite ores (substring — variant chunks)
            new WeightRule { Pattern = "*-poor-cassiterite-*",      Weight = 13,  Kind = "item" },
            new WeightRule { Pattern = "*-medium-cassiterite-*",    Weight = 26,  Kind = "item" },
            new WeightRule { Pattern = "*-rich-cassiterite-*",      Weight = 43,  Kind = "item" },
            new WeightRule { Pattern = "*-bountiful-cassiterite-*", Weight = 52,  Kind = "item" },

            new WeightRule { Pattern = "*-poor-hematite-*",      Weight = 52,  Kind = "item" },
            new WeightRule { Pattern = "*-medium-hematite-*",    Weight = 75,  Kind = "item" },
            new WeightRule { Pattern = "*-rich-hematite-*",      Weight = 78,  Kind = "item" },
            new WeightRule { Pattern = "*-bountiful-hematite-*", Weight = 104, Kind = "item" },

            new WeightRule { Pattern = "*-poor-quartz_nativesilver-*",      Weight = 13, Kind = "item" },
            new WeightRule { Pattern = "*-medium-quartz_nativesilver-*",    Weight = 26, Kind = "item" },
            new WeightRule { Pattern = "*-rich-quartz_nativesilver-*",      Weight = 43, Kind = "item" },
            new WeightRule { Pattern = "*-bountiful-quartz_nativesilver-*", Weight = 52, Kind = "item" },

            new WeightRule { Pattern = "*-poor-quartz_nativegold-*",      Weight = 13, Kind = "item" },
            new WeightRule { Pattern = "*-medium-quartz_nativegold-*",    Weight = 26, Kind = "item" },
            new WeightRule { Pattern = "*-rich-quartz_nativegold-*",      Weight = 43, Kind = "item" },
            new WeightRule { Pattern = "*-bountiful-quartz_nativegold-*", Weight = 52, Kind = "item" },

            // Generic ore tiers (prefix to catch ore-medium-iron etc.)
            new WeightRule { Pattern = "game:ore-medium*",    Weight = 19.5f, Kind = "item" },
            new WeightRule { Pattern = "game:ore-rich*",      Weight = 32.5f, Kind = "item" },
            new WeightRule { Pattern = "game:ore-bountiful*", Weight = 52f,   Kind = "item" },

            // Was WEIGHTS_FOR_ENDS_WITH
            new WeightRule { Pattern = "*ore-poor", Weight = 170, Kind = "item" },

            // Metal plates (exact)
            new WeightRule { Pattern = "game:metalplate-copper",         Weight = 178, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-brass",          Weight = 170, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-tinbronze",      Weight = 152, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-bismuthbronze",  Weight = 158, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-blackbronze",    Weight = 180, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-iron",           Weight = 156, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-gold",           Weight = 386, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-lead",           Weight = 226, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-tin",            Weight = 144, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-chromium",       Weight = 142, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-platinum",       Weight = 430, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-titanium",       Weight = 90,  Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-zinc",           Weight = 140, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-silver",         Weight = 210, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-bismuth",        Weight = 194, Kind = "item" },
            new WeightRule { Pattern = "game:metalplate-molybdochalkos", Weight = 192, Kind = "item" },

            // Ingots (exact)
            new WeightRule { Pattern = "game:ingot-copper",         Weight = 89,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-brass",          Weight = 85,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-tinbronze",      Weight = 76,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-bismuthbronze",  Weight = 79,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-blackbronze",    Weight = 90,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-iron",           Weight = 78,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-gold",           Weight = 193, Kind = "item" },
            new WeightRule { Pattern = "game:ingot-lead",           Weight = 113, Kind = "item" },
            new WeightRule { Pattern = "game:ingot-tin",            Weight = 72,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-chromium",       Weight = 71,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-platinum",       Weight = 215, Kind = "item" },
            new WeightRule { Pattern = "game:ingot-titanium",       Weight = 45,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-zinc",           Weight = 70,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-silver",         Weight = 105, Kind = "item" },
            new WeightRule { Pattern = "game:ingot-bismuth",        Weight = 97,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-molybdochalkos", Weight = 98,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-steel",          Weight = 78,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-blistersteel",   Weight = 78,  Kind = "item" },
            new WeightRule { Pattern = "game:ingot-meteoriciron",   Weight = 78,  Kind = "item" },

            // Bonus carry capacity (was WEIGHTS_BONUS_ITEMS)
            new WeightRule { Pattern = "game:basket",     Weight = 3000,  Kind = "bonus" },
            new WeightRule { Pattern = "game:backpack",   Weight = 6000,  Kind = "bonus" },
            new WeightRule { Pattern = "game:linensack",  Weight = 5000,  Kind = "bonus" },
            new WeightRule { Pattern = "game:miningbag",  Weight = 10000, Kind = "bonus" },
        };
        public string HUD_POSITION { get; set; } = "saturationstatbar";

        public float WEIGHT_HUD_Y = 0;
        public float WEIGHT_HUD_X = 0;

        public bool USE_WEIGHT_ORACLE = false;

        public bool WEIGHT_ORACLE_DONE = false;
        public void AddDefaultValue()
        {
            INVENTORY_WEIGHT_PLAYER_SETTINGS = new List<Dictionary<string, object>>
        {
            { new Dictionary<string, object>{ { "InvtentoryName", "backpack" }, { "StartSlot", 4 }, { "WeightBonus", false } } },
            { new Dictionary<string, object>{ { "InvtentoryName", "backpack" }, { "EndSlot", 3 }, { "WeightBonus", true } } },
            { new Dictionary < string, object > {  { "InvtentoryName", "hotbar" }, { "WeightBonus", false } } },
            { new Dictionary < string, object > {  { "InvtentoryName", "character" }, { "WeightBonus", false } } },
            { new Dictionary < string, object > {  { "InvtentoryName", "character" }, { "WeightBonus", true } } },
            { new Dictionary < string, object > { { "InvtentoryName", "mouse" }, { "WeightBonus", false } } }
        };
        }
        public List<Dictionary<string, object>> INVENTORY_WEIGHT_PLAYER_SETTINGS;
    }
}
