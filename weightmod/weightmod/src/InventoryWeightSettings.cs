using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace weightmod.src
{
    public class InventoryWeightSettings
    {
        public string InvtentoryName { get; set; }
        public int startSlot;
        public int StartSlot { get { if (this.startSlot == -1) { return -1; } return startSlot; } set { startSlot = value; } }
        public int endSlot;
        public int EndSlot { get { if (this.endSlot == -1) { return -1; } return endSlot; } set { endSlot = value; } }
        public bool WeightBonus = false;

    }
}
