using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;

namespace weightmod.src
{

    public class Config
    {
        public float MAX_PLAYER_WEIGHT { get; set; } = 20000;

        public float WEIGH_PLAYER_THRESHOLD { get; set; } = 0.7f;

        public float RATIO_MIN_MAX_WEIGHT_PLAYER_HEALTH { get; set; } = 0.6f;

        public float ACCUM_TIME_WEIGHT_CHECK { get; set; } = 2f;

        public bool PERCENT_MODIFIER_USED_ON_RAW_WEIGHT { get; set; } = false;

        public string CLASS_WEIGHT_BONUS { get; set; } = "commoner:0;hunter:500;malefactor:-500;clockmaker:-1000;blackguard:2000;tailor:-2000";

        public float HOW_OFTEN_RECHECK { get; set; } = 10f;

        public string INFO_COLOR_WEIGHT { get; set; } = "#F0C20B";
        public string INFO_COLOR_WEIGHT_BONUS { get; set; } = "#1F920E";

        public OrderedDictionary<string, float> WEIGHTS_FOR_ITEMS { get; set; } = new OrderedDictionary<string, float>
        {
            { "game:crystalizedore-poor-", 43},
            { "game:crystalizedore-medium-", 52},
            { "game:crystalizedore-rich-", 75},
            { "game:crystalizedore-bountiful-", 91},
            { "game:-poor-cassiterite-", 13},
            { "game:-medium-cassiterite-", 26},
            { "game:-rich-cassiterite-", 43},
            { "game:-bountiful-cassiterite-", 52},

            { "game:-poor-hematite-", 52},
            { "game:-medium-hematite-", 75},
            { "game:-rich-hematite-", 78},
            { "game:-bountiful-hematite-", 104},

            { "game:-poor-quartz_nativesilver-", 13},
            { "game:-medium-quartz_nativesilver-", 26},
            { "game:-rich-quartz_nativesilver-", 43},
            { "game:-bountiful-quartz_nativesilver-", 52},

            { "game:-poor-quartz_nativegold-", 13},
            { "game:-medium-quartz_nativegold-", 26},
            { "game:-rich-quartz_nativegold-", 43},
            { "game:-bountiful-quartz_nativegold-", 52},
            { "game:ore-medium", 19.5f},
            { "game:ore-rich", 32.5f},
            { "game:ore-bountiful", 52f},

            { "game:metalplate-copper", 178f},
            { "game:metalplate-brass", 170f},
            { "game:metalplate-tinbronze", 152f},
            { "game:metalplate-bismuthbronze", 158f},
            { "game:metalplate-blackbronze", 180f},
            { "game:metalplate-iron", 156f},
            { "game:metalplate-gold", 386f},
            { "game:metalplate-lead", 226f},
            { "game:metalplate-tin", 144f},
            { "game:metalplate-chromium", 142f},
            { "game:metalplate-platinum", 430f},
            { "game:metalplate-titanium", 90f},
            { "game:metalplate-zinc", 140f},
            { "game:metalplate-silver", 210f},
            { "game:metalplate-bismuth", 194f},
            { "game:metalplate-molybdochalkos", 192f},

            { "game:ingot-copper", 89f},
            { "game:ingot-brass", 85f},
            { "game:ingot-tinbronze", 76f},
            { "game:ingot-bismuthbronze", 79f},
            { "game:ingot-blackbronze", 90f},
            { "game:ingot-iron", 78f},
            { "game:ingot-gold", 193f},
            { "game:ingot-lead", 113f},
            { "game:ingot-tin", 72f},
            { "game:ingot-chromium", 71f},
            { "game:ingot-platinum", 215f},
            { "game:ingot-titanium", 45f},
            { "game:ingot-zinc", 70f},
            { "game:ingot-silver", 105},
            { "game:ingot-bismuth", 97f},
            { "game:ingot-molybdochalkos", 98f},
            { "game:ingot-steel", 78f},
            { "game:ingot-blistersteel", 78f},
            { "game:ingot-meteoriciron", 78f}

        };

        public Dictionary<string, int> WEIGHTS_FOR_BLOCKS { get; set; } = new Dictionary<string, int>
        {
            { "game:ore-poor", 170}
        };

        public Dictionary<string, int> WEIGHTS_FOR_ENDS_WITH { get; set; } = new Dictionary<string, int>
        {
            { "game:ore-poor", 170}
        };
        public Dictionary<string, int> WEIGHTS_BONUS_ITEMS { get; set; } = new Dictionary<string, int>
        {
            { "game:basket", 1000},
            { "game:backpack", 2000},
            { "game:linensack", 1300},
            { "game:miningbag", 3300}
        };
        public string HUD_POSITION { get; set; } = "saturationstatbar";
    }
}
