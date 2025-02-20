﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;
using weightmod.src.EB;

namespace weightmod.src.harmony
{
    [HarmonyPatch]
    public class harmPatch
    {
        public static void Postfix_GetHeldItemInfo(CollectibleObject __instance, ItemSlot inSlot,
                                                                                                         StringBuilder dsc,
                                                                                                         IWorldAccessor world,
                                                                                                         bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            if (itemstack.ItemAttributes != null && itemstack.ItemAttributes["weightmod"].Exists)
            {
                float tmp = itemstack.ItemAttributes["weightmod"].AsFloat();
                if (tmp <= 0) return;
                dsc.Append(string.Concat(new string[]
                    {
                        "<font color=",
                        weightmod.config.INFO_COLOR_WEIGHT,
                        ">",
                        Lang.Get("weightmod:item_weight", Array.Empty<object>()),
                        "</font>"
                    })).Append(itemstack.ItemAttributes["weightmod"].AsFloat(0f).ToString()).Append("\n");
                //dsc.Append("<font color=" + Config.Current.INFO_COLOR_WEIGHT.Val + ">" + "<icon name=bear></icon> "  + "</font>").Append(itemstack.ItemAttributes["weightmod"].AsFloat().ToString()).Append("\n");
            }
            else if (itemstack.ItemAttributes != null && itemstack.ItemAttributes["weightbonusbags"].Exists)
            {
                float tmp = itemstack.ItemAttributes["weightbonusbags"].AsFloat();
                if (tmp <= 0) return;
                dsc.Append(string.Concat(new string[]
                    {
                        "<font color=",
                        weightmod.config.INFO_COLOR_WEIGHT_BONUS,
                        ">",
                        Lang.Get("weightmod:bonus_weight", Array.Empty<object>()),
                        "</font>"
                    })).Append(itemstack.ItemAttributes["weightbonusbags"].AsFloat(0f).ToString()).Append("\n");
                //dsc.Append("<font color=" + Config.Current.INFO_COLOR_WEIGHT_BONUS.Val + ">" + "<icon name=basket></icon> " + "</font>").Append(itemstack.ItemAttributes["weightbonusbags"].AsFloat().ToString()).Append("\n");
            }

            return;
        }
        public static bool Prefix_ApplicableInAir(PModuleOnGround __instance, Entity entity, EntityPos pos, EntityControls controls)
        {
            if (!(entity is EntityPlayer))
            {
                return true;
            }
            EntityBehaviorWeightable beBeh = entity.GetBehavior<EntityBehaviorWeightable>();
            if (beBeh != null)
            {
                //EntityBehaviorControlledPhysics
                if (beBeh.isOverloaded())
                {
                    return false;
                }
            }
            return true;
        }
        public static bool Prefix_ApplicableInLiquid(PModulePlayerInLiquid __instance, Entity entity, EntityPos pos, EntityControls controls)
        {
            if (!(entity is EntityPlayer))
            {
                return true;
            }
            EntityBehaviorWeightable beBeh = entity.GetBehavior<EntityBehaviorWeightable>();
            if (beBeh != null)
            {
                //EntityBehaviorControlledPhysics
                if (beBeh.isOverloaded())
                {
                    entity.Pos.Motion.X = 0;
                    entity.Pos.Motion.Y = 0;
                    entity.Pos.Motion.Z = 0;
                    return false;
                }
            }
            return true;
        }
        public static bool Prefix_ApplicableOnGround(PModulePlayerInAir __instance, Entity entity, EntityPos pos, EntityControls controls)
        {
            if (!(entity is EntityPlayer))
            {
                return true;
            }
            EntityBehaviorWeightable beBeh = entity.GetBehavior<EntityBehaviorWeightable>();
            if (beBeh != null)
            {
                //EntityBehaviorControlledPhysics
                if (beBeh.isOverloaded())
                {
                    return false;
                }
            }
            return true;
        }

        public static void Prefix_OnItemSlotModified(InventoryBase __instance, ItemSlot slot)
        {
            if (weightmod.sapi == null)
            {
                return;
            }
            if (__instance is InventoryBasePlayer)
            {
                var a = __instance.Api.World.PlayerByUid((__instance as InventoryBasePlayer).Player.PlayerUID).Entity.GetBehavior<EntityBehaviorWeightable>();
                if (a != null)
                {
                    a.shouldRecalc = true;
                }
            }
        }


    }
}
