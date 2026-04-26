using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace weightmod.src
{
    public class WeightOracle
    {
        private ICoreAPI api;
        private ItemCategorizer itemCategorizer;
        Config config;
        private readonly string configFileName;
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
            api.StoreModConfig<Config>(weightmod.config, configFileName);
            weightmod.config.WEIGHT_ORACLE_DONE = true;

        }
        private void ProcessCollectibleWeight(CollectibleObject collectibleObject, bool forItem)
        {
            if (collectibleObject?.Code == null) return;

            string collectibleCode = collectibleObject.Code.ToString();


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
