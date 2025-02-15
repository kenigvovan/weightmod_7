using Vintagestory.API.Common;

namespace weightmod.src
{
    public class ItemCategorizer
    {
        private readonly Config config;
        private readonly ICoreAPI api;

        public ItemCategorizer(ICoreAPI api, Config config)
        {
            this.api = api;
            this.config = config;
        }
        public string DetermineCategory(CollectibleObject item)
        {
            if (item == null) return "misc";

            // Vérification du type d'item
            if (item.Tool != null) return "tool";
            if (item.Code.Path.Contains("weapon")) return "weapon";
            if (item.Code.Path.Contains("armor")) return "armor";

            // Vérification des items de nourriture par attributs
            if (item.Attributes != null &&
                (item.Code.Path.Contains("food") ||
                 item.Attributes["nutrition"].Exists)) return "food";

            // Vérification des attributs d'item
            var attributes = item.Attributes;
            if (attributes != null)
            {
                string path = item.Code.Path.ToLower();
                if (path.Contains("ore")) return "ore";
                if (path.Contains("ingot") || path.Contains("metal")) return "metal";
                if (path.Contains("gem")) return "gem";

                string material = attributes["material"]?.AsString()?.ToLower();
                if (!string.IsNullOrEmpty(material))
                {
                    if (material.Contains("wood")) return "wood";
                    if (material.Contains("stone")) return "stone";
                    if (material.Contains("metal")) return "metal";
                }
            }

            // Vérification spécifique pour les blocs
            if (item is Block block)
            {
                if (block.BlockMaterial == EnumBlockMaterial.Wood) return "woodblock";
                if (block.BlockMaterial == EnumBlockMaterial.Stone) return "stoneblock";
                if (block.BlockMaterial == EnumBlockMaterial.Metal) return "metalblock";
                if (block.BlockMaterial == EnumBlockMaterial.Glass) return "glassblock";
            }

            return "misc";
        }

        public float CalculateBaseWeight(CollectibleObject item, string category)
        {
            float baseWeight = config.BASE_WEIGHTS_BY_CATEGORY.ContainsKey(category)
                ? config.BASE_WEIGHTS_BY_CATEGORY[category]
                : 200f;

            // Multiplicateurs basés sur le matériau
            if (item.Attributes != null)
            {
                string material = item.Attributes["material"]?.AsString()?.ToLower();
                if (!string.IsNullOrEmpty(material))
                {
                    foreach (var multiplier in config.MATERIAL_MULTIPLIERS)
                    {
                        if (material.Contains(multiplier.Key))
                        {
                            baseWeight *= multiplier.Value;
                            break;
                        }
                    }
                }
            }

            // Ajustements spéciaux pour les blocs
            if (item is Block)
            {
                string path = item.Code.Path.ToLower();
                if (path.Contains("stairs") || path.Contains("slab"))
                {
                    baseWeight *= 0.5f;
                }
                else if (path.Contains("wall"))
                {
                    baseWeight *= 0.8f;
                }
                else if (path.Contains("fence") || path.Contains("gate"))
                {
                    baseWeight *= 0.3f;
                }
            }

            // Ajustement pour les items stackables
            if (item.MaxStackSize > 1)
            {
                baseWeight *= 0.8f;
            }
            AddToConfig(item, baseWeight);
            return baseWeight;
        }

        public void AddToConfig(CollectibleObject item, float weight)
        {
            if (item?.Code == null) return;

            string itemCode = item.Code.ToString();

            if (item is Block)
            {
                if (!config.WEIGHTS_FOR_BLOCKS.ContainsKey(itemCode))
                {
                    config.WEIGHTS_FOR_BLOCKS[itemCode] = weight;
                }
            }
            else
            {
                if (!config.WEIGHTS_FOR_ITEMS.ContainsKey(itemCode))
                {
                    config.WEIGHTS_FOR_ITEMS[itemCode] = weight;
                }
            }
        }
    }
}
