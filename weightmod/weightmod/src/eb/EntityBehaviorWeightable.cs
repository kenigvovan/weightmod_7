using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace weightmod.src.eb
{
    public abstract class EntityBehaviorWeightable : EntityBehavior
    {
        protected float currentCalculatedWeight = 0;
        protected float lastCalculatedWeight = 0;
        protected ITreeAttribute weightTree;
        public bool shouldRecalc = false;
        public bool shouldUpdate = true;
        float accum = 0;
        public static Config config;
        protected EntityBehaviorWeightable(Entity entity) : base(entity)
        {
        }
        public abstract bool isOverloaded();
        public float maxWeight
        {
            get { return weightTree.GetFloat("maxweight"); }
            set
            {
                weightTree.SetFloat("maxweight", value);
                entity.WatchedAttributes.MarkPathDirty("weightmod");
            }
        }

        public float weight
        {
            get { return weightTree.GetFloat("currentweight"); }
            set { weightTree.SetFloat("currentweight", value); entity.WatchedAttributes.MarkPathDirty("weightmod"); }
        }
        public override void AfterInitialized(bool onFirstSpawn)
        {
            base.AfterInitialized(onFirstSpawn);
            weightTree = entity.WatchedAttributes.GetTreeAttribute("weightmod");
        }
        public abstract void updateWeight();
        protected void OnSlotModified(int i)
        {
            shouldRecalc = true;
        }
        public override void OnGameTick(float deltaTime)
        {
            if (entity.World.Api.Side == EnumAppSide.Server)
            {
                accum += deltaTime;
                if (accum >= config.HOW_OFTEN_RECHECK && shouldRecalc)
                {
                    shouldRecalc = false;
                    accum = 0;
                    updateWeight();
                }
            }
        }
        public void MarkDirty()
        {
            entity.WatchedAttributes.MarkPathDirty("weightmod");
        }
        public override void GetInfoText(StringBuilder infotext)
        {
            base.GetInfoText(infotext);
            infotext.AppendLine("Weight: 2");
        }
    }
}
