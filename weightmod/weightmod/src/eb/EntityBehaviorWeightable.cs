using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace weightmod.src.eb
{
    public abstract class EntityBehaviorWeightable : EntityBehavior
    {
        protected const float FLOAT_EPSILON = 0.001f;
        protected float currentCalculatedWeight = 0;
        protected float lastCalculatedWeight = 0;
        protected ITreeAttribute weightTree;
        public bool shouldUpdate = true;
        public static Config config;

        private bool _shouldRecalc = false;
        private float debounceAccum = 0;
        private float burstAccum = 0;

        // Debounce trigger: every slot event resets the debounce timer; only the
        // first event of a burst resets burstAccum. OnGameTick then recalculates
        // either when the debounce window passed without new events, or when the
        // burst exceeded HOW_OFTEN_RECHECK (cap for continuous event streams).
        public bool shouldRecalc
        {
            get => _shouldRecalc;
            set
            {
                if (value)
                {
                    debounceAccum = 0;
                    if (!_shouldRecalc) burstAccum = 0;
                }
                _shouldRecalc = value;
            }
        }

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
            if (entity.World.Api.Side != EnumAppSide.Server) return;
            if (!_shouldRecalc) return;

            debounceAccum += deltaTime;
            burstAccum    += deltaTime;

            if (debounceAccum >= config.RECALC_DEBOUNCE_SECONDS ||
                burstAccum    >= config.HOW_OFTEN_RECHECK)
            {
                _shouldRecalc = false;
                updateWeight();
            }
        }
        public void MarkDirty()
        {
            entity.WatchedAttributes.MarkPathDirty("weightmod");
        }
    }
}
