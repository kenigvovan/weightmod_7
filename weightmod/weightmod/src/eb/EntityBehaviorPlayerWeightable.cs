using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using weightmod.src.eb;

namespace weightmod.src.EB
{
    public class EntityBehaviorPlayerWeightable : EntityBehaviorWeightable
    {
        bool changeMade = true;

        ITreeAttribute healthTree;
        float lastRatioHealth;
        float lastWeightBonusBags = 0;
        float currentWeightBonusBags = 0;
        float classBonus = 0;
        public bool InventoryHandlingInit = false;
        Random random = new Random();
        public float lastWeightBonus
        {
            get { return weightTree.GetFloat("weightbonus"); }
            set
            {
                weightTree.SetFloat("weightbonus", value);
                entity.WatchedAttributes.MarkPathDirty("weightmod");
            }
        }
        public float lastPercentModifier
        {
            get { return weightTree.GetFloat("percentmodifier"); }
            set
            {
                weightTree.SetFloat("percentmodifier", value);
                entity.WatchedAttributes.MarkPathDirty("weightmod");
            }
        }

        public static WeightStorage weightStorage;
        public static InventoryWeightSettings[] PlayerWeightSettings { get; set; }  
        
        public void InitInventoryHandling()
        {
            if (entity.World.Side == EnumAppSide.Server && !this.InventoryHandlingInit)
            {
                List<string> mem = new List<string>();
                foreach (var inv in PlayerWeightSettings)
                {
                    if (mem.Contains(inv.InvtentoryName))
                    {
                        continue;
                    }
                    mem.Add(inv.InvtentoryName);
                    var invObject = (InventoryBasePlayer)((entity as EntityPlayer).Player as IServerPlayer).InventoryManager.GetOwnInventory(inv.InvtentoryName);
                    if (invObject == null)
                    {
                        break;
                    }
                    invObject.SlotModified += OnSlotModified;
                    this.InventoryHandlingInit = true;
                }
            }
        }

        public override void AfterInitialized(bool onFirstSpawn)
        {
            base.AfterInitialized(onFirstSpawn);
           
            healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
            string playerClass = entity.WatchedAttributes.GetString("characterClass");
            if (playerClass == null || !weightmod.getclassBonuses().TryGetValue(playerClass, out classBonus))
            {
                classBonus = 0;
            }

            InitInventoryHandling();

            if (weightTree == null)
            {
                entity.WatchedAttributes.SetAttribute("weightmod", weightTree = new TreeAttribute());

                weight = 0;
                maxWeight = config.MAX_PLAYER_WEIGHT;
                lastPercentModifier = 1;
                lastWeightBonusBags = 0;
                lastWeightBonus = 0;

                MarkDirty();
                return;
            }

            if (healthTree == null && entity.World.Side == EnumAppSide.Server)
            {
                healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
                lastRatioHealth = healthTree.GetFloat("currenthealth") / healthTree.GetFloat("maxhealth");
                lastRatioHealth = 1;
            }
            else
            {
                lastRatioHealth = healthTree.GetFloat("currenthealth") / healthTree.GetFloat("maxhealth");
            }
            lastPercentModifier = weightTree.GetFloat("percentmodifier");
            weight = weightTree.GetFloat("currentweight");
            maxWeight = config.MAX_PLAYER_WEIGHT;
            lastWeightBonusBags = 0;
            lastWeightBonus = weightTree.GetFloat("weightbonus");

            MarkDirty();
        }

        public EntityBehaviorPlayerWeightable(Entity entity) : base(entity)
        {

        }
        public override bool isOverloaded()
        {
            return weight > maxWeight;
        }
        public override string PropertyName()
        {
            return "affectedByItemsWeight";
        }
        public bool calculateWeightOfInventories()
        {
            if ((entity as EntityPlayer).Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                currentCalculatedWeight = 0;
                return false;
            }
            currentCalculatedWeight = 0;
            currentWeightBonusBags = 0;
            shouldUpdate = false;

            foreach(var inv in EntityBehaviorPlayerWeightable.PlayerWeightSettings)
            {
                IInventory invObject = ((entity as EntityPlayer).Player as IServerPlayer).InventoryManager.GetOwnInventory(inv.InvtentoryName);
                if (invObject == null)
                {
                    continue;
                }
                int i = inv.startSlot == -1 ? 0 : inv.startSlot;
                int end = inv.endSlot == -1 ? invObject.Count : inv.endSlot;
                for (; i < end; i++)
                {
                    ItemSlot itemSlot = invObject[i];
                    if (itemSlot != null)
                    {
                        ItemStack itemStack = itemSlot.Itemstack;
                        if (itemStack != null && itemStack.Collectible != null)
                        {
                            if (inv.WeightBonus)
                            {
                                if (itemStack.Collectible.Attributes != null && itemStack.Collectible.Attributes["weightbonusbags"].Exists)
                                {
                                    currentWeightBonusBags += itemStack.Collectible.Attributes["weightbonusbags"].AsFloat();
                                    continue;
                                }
                            }
                            else
                            {
                                if (itemStack.Collectible.Attributes != null && itemStack.Collectible.Attributes["weightmod"].Exists)
                                {
                                    currentCalculatedWeight += itemStack.Collectible.Attributes["weightmod"].AsFloat() * itemStack.StackSize;
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
   
            if (currentCalculatedWeight < 0)
            {
                currentCalculatedWeight = 0;
            }

            return shouldUpdate;
        }
        public override void updateWeight()
        {
            //currentCalculatedWeight updated
            calculateWeightOfInventories();

            ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("weightmod");

            if (treeAttribute == null)
            {
                return;
            }

            //if weight was changed
            if (!shouldUpdate && currentCalculatedWeight - lastCalculatedWeight > 20)
            {
                shouldUpdate = true;
            }
            // health ration changed
            if (!shouldUpdate && lastPercentModifier - treeAttribute.GetFloat("percentmodifier") > 0.09)
            {
                changeMade = true;
                lastPercentModifier = treeAttribute.GetFloat("percentmodifier");
            }
            if (!shouldUpdate && lastWeightBonus != entity.Stats.GetBlended("weightmodweightbonus"))
            {
                changeMade = true;
                lastWeightBonus = entity.Stats.GetBlended("weightmodweightbonus");
            }

            if (!shouldUpdate && lastWeightBonusBags != currentWeightBonusBags)
            {
                changeMade = true;
                lastWeightBonusBags = currentWeightBonusBags;
            }

            if (changeMade || shouldUpdate)
            {
                if (!config.PERCENT_MODIFIER_USED_ON_RAW_WEIGHT)
                    maxWeight = (config.MAX_PLAYER_WEIGHT * lastRatioHealth + lastWeightBonusBags + lastWeightBonus + classBonus) * lastPercentModifier;
                else
                    maxWeight = config.MAX_PLAYER_WEIGHT * lastPercentModifier * lastRatioHealth + lastWeightBonusBags + lastWeightBonus + classBonus;
            }

            if (maxWeight < config.MAX_PLAYER_WEIGHT * config.RATIO_MIN_MAX_WEIGHT_PLAYER_HEALTH)
            {
                maxWeight = config.MAX_PLAYER_WEIGHT * config.RATIO_MIN_MAX_WEIGHT_PLAYER_HEALTH;
            }

            if (currentCalculatedWeight > maxWeight)
            {
                //Processed in harmPatch (Prefix_DoApplyOnGround/Prefix_DoApplyInLiquid) using isOverloaded
                entity.Stats.Set("walkspeed", "weightmod", -2, true);
                if ((entity as EntityAgent).MountedOn != null)
                {
                    (entity as EntityAgent).TryUnmount();
                }
            }
            //when player is not overburden yet but currentweight is across threshold value from config,
            //slower movespeed
            else if (currentCalculatedWeight > maxWeight * config.WEIGH_PLAYER_THRESHOLD)
            {
                entity.Stats.Set("walkspeed", "weightmod", (float)(-0.2 * (currentCalculatedWeight / maxWeight)), true);
            }
            else
            {
                entity.Stats.Set("walkspeed", "weightmod", 0);
            }


            //if health ratio was changed or current and last weight is not the same = send packet and also update
            //treeAttribute with maxweight and currentweight
            if (changeMade || lastCalculatedWeight != currentCalculatedWeight)
            {
                treeAttribute.SetFloat("currentweight", currentCalculatedWeight);
                treeAttribute.SetFloat("maxweight", maxWeight);
                entity.WatchedAttributes.MarkPathDirty("weightmod");
                //(entity.Api as ICoreServerAPI).Network.SendEntityPacket((entity as EntityPlayer).Player as IServerPlayer, entity.EntityId, 6166, SerializerUtil.ToBytes((w) => treeAttribute.ToBytes(w)));
            }

            //current now last
            lastCalculatedWeight = currentCalculatedWeight;
            changeMade = false;
        }    
        public override void OnEntityRevive()
        {
            if (entity.World.Side == EnumAppSide.Server)
            {
                updateWeight();
                if (currentCalculatedWeight < 0)
                {
                    currentCalculatedWeight = 0;
                }
                if ((entity as EntityPlayer).Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
                {
                    currentCalculatedWeight = 0;
                }
            }
        }
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            base.OnEntityReceiveDamage(damageSource, ref damage);
            if (healthTree == null)
            {
                return;
            }
            float tmpHealthRation = healthTree.GetFloat("currenthealth") / healthTree.GetFloat("maxhealth");
            if (lastRatioHealth != tmpHealthRation)
            {
                shouldRecalc = true;
                changeMade = true;
                lastRatioHealth = tmpHealthRation;
            }
        }
    }
}
