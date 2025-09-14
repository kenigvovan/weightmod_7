using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
using weightmod.src.eb;
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
        public static void Postfix_getOrCreateContainerWorkspace(AttachedContainerWorkspace __instance, ItemSlot bagSlot, int slotIndex, Entity entity)
        {
            var ebew = entity.GetBehavior<EntityBehaviorElkWeightable>();
            if(ebew != null && !ebew.Initialized)                
            {
                __instance.WrapperInv.SlotModified += ebew.InnerSlotsModified;
                ebew.Initialized = true;
                /*if(__result != null && __result.WrapperInv != null)
                {
                    ebew.Initialized = true;
                    __result.WrapperInv.SlotModified += ebew.InnerSlotsModified;
                }*/
            }
            return;
        }
        public static void AddSlotModified(AttachedContainerWorkspace ws)
        {
            var ebew = ws.entity.GetBehavior<EntityBehaviorElkWeightable>();
            if (ebew != null)
            {
                ws.WrapperInv.SlotModified += ebew.InnerSlotsModified;
                ebew.Initialized = true;
            }
        }
        public static IEnumerable<CodeInstruction> Transpiler_TryLoadInv(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool found = false;
            bool foundSec = false;
            var codes = new List<CodeInstruction>(instructions);
            var proxyMethod = AccessTools.Method(typeof(harmPatch), "AddSlotModified");
            for (int i = 0; i < codes.Count; i++)
            {

                if (!found &&
                        codes[i].opcode == OpCodes.Ldfld && codes[i + 1].opcode == OpCodes.Ldarg_0 && codes[i + 2].opcode == OpCodes.Ldftn && codes[i - 1].opcode == OpCodes.Ldarg_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, proxyMethod);
                    found = true;
                }
                yield return codes[i];
            }
        }
        public static bool Prefix_ApplicableInAir(PModuleOnGround __instance, Entity entity, EntityPos pos, EntityControls controls)
        {
            if (!(entity is EntityPlayer))
            {
                return true;
            }
            EntityBehaviorPlayerWeightable beBeh = entity.GetBehavior<EntityBehaviorPlayerWeightable>();
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
            EntityBehaviorPlayerWeightable beBeh = entity.GetBehavior<EntityBehaviorPlayerWeightable>();
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
            EntityBehaviorPlayerWeightable beBeh = entity.GetBehavior<EntityBehaviorPlayerWeightable>();
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
                var a = __instance.Api.World.PlayerByUid((__instance as InventoryBasePlayer).Player.PlayerUID).Entity.GetBehavior<EntityBehaviorPlayerWeightable>();
                if (a != null)
                {
                    a.shouldRecalc = true;
                }
            }
        }


    }
}
