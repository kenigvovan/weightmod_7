using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using weightmod.src.EB;

namespace weightmod.src.eb
{
    public class EntityBehaviorElkWeightable : EntityBehaviorWeightable
    {
        public bool Initialized = false;
        public int[] SlotsToCheck = new int[0];
        public EntityBehaviorElkWeightable(Entity entity) : base(entity)
        {
        }
        public override bool isOverloaded()
        {
            return weight > maxWeight;
        }
        public override string PropertyName()
        {
            return "affectedByItemsWeightElk";
        }
        public override void AfterInitialized(bool onFirstSpawn)
        {
            base.AfterInitialized(onFirstSpawn);

            if (weightTree == null)
            {
                entity.WatchedAttributes.SetAttribute("weightmod", weightTree = new TreeAttribute());

                weight = 0;
                maxWeight = config.MAX_ELK_WEIGHT;

                MarkDirty();
                //return;
            }
            EntityBehaviorAttachable ebc = this.entity.GetBehavior<EntityBehaviorAttachable>();
            if (ebc != null)
            {
                WearableSlotConfig[] slotsConfigs = (WearableSlotConfig[])typeof(EntityBehaviorAttachable).GetField("wearableSlots", System.Reflection.BindingFlags.NonPublic |
                              System.Reflection.BindingFlags.Instance).GetValue(ebc);
                int i = 0;
                foreach (var it in slotsConfigs)
                {
                    if (it.ForCategoryCodes.Contains("chest") || it.ForCategoryCodes.Contains("basket") || it.ForCategoryCodes.Contains("storage") || it.ForCategoryCodes.Contains("sidebags"))
                    {
                        this.SlotsToCheck = this.SlotsToCheck.Append(i);
                    }
                    i++;
                }
               
            }
        }
        public void RealSlotsModified(int slotId)
        {
            if (this.SlotsToCheck.Contains(slotId))
            {
                shouldRecalc = true;
            }
        }
        public void InnerSlotsModified(int slotId)
        {
            shouldRecalc = true;
        }
        public override void updateWeight()
        {
            EntityBehaviorAttachable ebc = this.entity.GetBehavior<EntityBehaviorAttachable>();
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

            if (currentCalculatedWeight > maxWeight)
            {
                if (this.entity.Api.Side == EnumAppSide.Server)
                {
                    if (ebc != null)
                        foreach (var slId in this.SlotsToCheck)
                        {
                            {
                                if (ebc.Inventory[slId].Itemstack != null)
                                {
                                    var beh = ebc.Inventory[slId].Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorHeldBag>(true);
                                    if (beh == null)
                                    {
                                        continue;
                                    }
                                    var ws = beh.getContainerWorkspace(slId, this.entity);
                                    if (ws != null)
                                    {
                                        ws.WrapperInv.DropAll(this.entity.Pos.AsBlockPos.ToVec3d());
                                    }
                                }
                            }
                        }
                }
            }

            //if health ratio was changed or current and last weight is not the same = send packet and also update
            //treeAttribute with maxweight and currentweight
            if (lastCalculatedWeight != currentCalculatedWeight)
            {
                treeAttribute.SetFloat("currentweight", currentCalculatedWeight);
                treeAttribute.SetFloat("maxweight", maxWeight);
                entity.WatchedAttributes.MarkPathDirty("weightmod");
                //(entity.Api as ICoreServerAPI).Network.SendEntityPacket((entity as EntityPlayer).Player as IServerPlayer, entity.EntityId, 6166, SerializerUtil.ToBytes((w) => treeAttribute.ToBytes(w)));
            }

            //current now last
            lastCalculatedWeight = currentCalculatedWeight;
        }
        public bool calculateWeightOfInventories()
        {
            EntityBehaviorAttachable ebc = this.entity.GetBehavior<EntityBehaviorAttachable>();
            if (ebc == null)
            {
                return false;
            }
            currentCalculatedWeight = 0;
            if (this.entity.Api.Side == EnumAppSide.Server)
            {
                if (ebc != null)
                {
                    foreach (var slId in this.SlotsToCheck)
                    {
                        if (ebc.Inventory[slId].Itemstack != null)
                        {
                            var beh = ebc.Inventory[slId].Itemstack.Collectible.GetCollectibleBehavior<CollectibleBehaviorHeldBag>(true);
                            if(beh == null)
                            {
                                continue;
                            }
                            foreach (var it in beh.getContainerWorkspace(slId, this.entity).WrapperInv)
                            {
                                ItemSlot itemSlot = it;
                                if (itemSlot != null)
                                {
                                    ItemStack itemStack = itemSlot.Itemstack;
                                    if (itemStack != null && itemStack.Collectible != null)
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
                }
                if (currentCalculatedWeight < 0)
                {
                    currentCalculatedWeight = 0;
                }

                return true;
            }
            return false;
        }
    }
}
