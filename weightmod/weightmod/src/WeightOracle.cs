using System.Collections.Generic;
using Vintagestory.API.Common;

namespace weightmod.src
{
    public class WeightOracle
    {
        private ICoreAPI api;
        private ItemCategorizer itemCategorizer;
        Config config;
        private readonly string configFileName;
        private List<CompiledRule> bonusRules;
        public WeightOracle(ICoreAPI api, Config config, string configFileName)
        {
            this.api = api;
            itemCategorizer = new(api, config);
            this.config = config;
            this.configFileName = configFileName;
        }
        public void FillConfigDicts()
        {
            int itemCount = 0;
            int blockCount = 0;

            bonusRules = new List<CompiledRule>();
            if (config.WEIGHT_RULES != null)
            {
                foreach (var r in config.WEIGHT_RULES)
                {
                    var c = CompiledRule.TryParse(r);
                    if (c != null && c.Kind == RuleKind.Bonus) bonusRules.Add(c);
                }
            }

            foreach (var item in api.World.Items)
            {
                if (item?.Code == null) continue;

                ProcessCollectibleWeight(item, forItem: true);

                itemCount++;
            }

            api.Logger.VerboseDebug($"Processed {itemCount} items");

            foreach (var block in api.World.Blocks)
            {
                if (block?.Code == null) continue;

                ProcessCollectibleWeight(block, forItem: false);
                blockCount++;
            }

            api.Logger.VerboseDebug($"Processed {blockCount} blocks");

            WeightCompactor.Compact(config, api);

            weightmod.config.WEIGHT_ORACLE_DONE = true;
            api.StoreModConfig<Config>(weightmod.config, configFileName);         
        }
        private void ProcessCollectibleWeight(CollectibleObject collectibleObject, bool forItem)
        {
            if (collectibleObject?.Code == null) return;

            string collectibleCode = collectibleObject.Code.ToString();

            if (config.ORACLE_CODE_BLACKLIST != null)
            {
                for (int i = 0; i < config.ORACLE_CODE_BLACKLIST.Count; i++)
                {
                    var needle = config.ORACLE_CODE_BLACKLIST[i];
                    if (!string.IsNullOrEmpty(needle) && collectibleCode.Contains(needle)) return;
                }
            }

            if (forItem && bonusRules != null)
            {
                foreach (var br in bonusRules)
                {
                    if (br.Matches(collectibleCode)) return;
                }
            }


            if ((forItem && !config.WEIGHTS_FOR_ITEMS.ContainsKey(collectibleCode)) ||
            (!forItem && !config.WEIGHTS_FOR_BLOCKS.ContainsKey(collectibleCode)))
            {
                string category = itemCategorizer.DetermineCategory(collectibleObject);
                var baseWeight = itemCategorizer.CalculateBaseWeight(collectibleObject, category);

                if(forItem)
                {
                    this.config.WEIGHTS_FOR_ITEMS[collectibleCode] = baseWeight;
                }
                else
                {
                    this.config.WEIGHTS_FOR_BLOCKS[collectibleCode] = baseWeight;
                }              
                api.Logger.VerboseDebug($"Added block weight: {collectibleObject} = {baseWeight} into config.");
            }
        }
   

    }
}
