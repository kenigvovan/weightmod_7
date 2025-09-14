using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;

namespace weightmod.src.eb
{
    public class EntityBehaviorShowWeightInfo : EntityBehavior
    {
        int currentWeight = 0;
        public EntityBehaviorShowWeightInfo(Entity entity) : base(entity)
        {
            this.entity.WatchedAttributes.RegisterModifiedListener("weightmod", () =>
            {
                var tree = entity.WatchedAttributes.GetTreeAttribute("weightmod");
                if (tree != null)
                {
                    currentWeight = (int)tree.GetFloat("currentweight");
                }
            });
        }

        public override string PropertyName()
        {
            return "showweightinfo";
        }
        public override void GetInfoText(StringBuilder infotext)
        {
            base.GetInfoText(infotext);
            infotext.AppendLine("Weight: " + currentWeight);
        }
    }
}
