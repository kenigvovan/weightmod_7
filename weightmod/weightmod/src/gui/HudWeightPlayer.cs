using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace weightmod.src.gui
{
    public class HudWeightPlayer : HudElement
    {
        private float lastWeight;
        private float lastMaxWeight;
        GuiElementStatbar weightBar;
        public override double InputOrder => 1.0;
        public HudWeightPlayer(ICoreClientAPI capi) : base(capi)
        {

        }

        private void UpdateWeight()
        {
            ITreeAttribute treeAttribute = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("weightmod");
            if (treeAttribute == null)
                return;
            float? nullable1 = treeAttribute.TryGetFloat("currentweight");
            float? nullable2 = treeAttribute.TryGetFloat("maxweight");
            if (!nullable1.HasValue || !nullable2.HasValue)
                return;
            double lastWeight = this.lastWeight;
            float? nullable3 = nullable1;
            double valueOrDefault1 = (double)nullable3.GetValueOrDefault();
            if (lastWeight == valueOrDefault1 & nullable3.HasValue)
            {
                double lastMaxHealth = lastMaxWeight;
                float? nullable4 = nullable2;
                double valueOrDefault2 = (double)nullable4.GetValueOrDefault();
                if (lastMaxHealth == valueOrDefault2 & nullable4.HasValue)
                    return;
            }
            if (weightBar == null)
            {
                ComposeGuis();
                return;
            }
            weightBar.SetLineInterval(1f);
            weightBar.SetValues(nullable1.Value, 0.0f, nullable2.Value);
            this.lastWeight = nullable1.Value;
            lastMaxWeight = nullable2.Value;
            //ComposeGuis();
        }
        public override void OnOwnPlayerDataReceived()
        {
            ComposeGuis();
            UpdateWeight();
            capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("weightmod", () => UpdateWeight());
        }
        public void ComposeGuis()
        {
            IRenderAPI render = capi.Render;

            bool thirstBarFound = false;
            //GuiElementStatbar thirstBar = null;
            foreach (var gui in weightmod.capi.Gui.LoadedGuis)
            {
                if (gui.DebugName == "ThirstBarHudElement")
                {
                    thirstBarFound = true;
                    //tmpBar = gui.Composers["statbar"].GetStatbar("healthstatbar");
                }
            }

            if (weightmod.config.HUD_POSITION == "saturationstatbar")
            {
                float num = 850f;
                ElementBounds bounds1 = new ElementBounds()
                {
                    Alignment = EnumDialogArea.CenterBottom,
                    BothSizing = ElementSizing.Fixed,
                    fixedWidth = num,
                    fixedHeight = 100
                }.WithFixedAlignmentOffset(weightmod.config.WEIGHT_HUD_X, weightmod.config.WEIGHT_HUD_Y);

                if (thirstBarFound)
                {
                    bounds1.fixedX += 500;
                    bounds1.fixedY -= 5;
                }
                else
                {
                    bounds1.fixedX += 500;
                    bounds1.fixedY += 5;
                }
                //bounds1.fixedY -= weightmod.Config.WEIGHT_HUD_Y;
                Composers["weightbar"] = capi.Gui.CreateCompo("weight-statbar", bounds1.FlatCopy().FixedGrow(0.0, 20.0));

                ElementBounds bounds2 = ElementStdBounds.Statbar(EnumDialogArea.LeftTop, (double)num * 0.41);
                bounds2.WithFixedHeight(10.0);

                ITreeAttribute treeAttribute2 = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("weightmod");

                var weightStatBar = new GuiElementStatbar(capi, bounds2, GuiStyle.XPBarColor, false, false);

                Composers["weightbar"]//.BeginChildElements(bounds1)
                                           .AddInteractiveElement(weightStatBar, "weightstatbar")
                                           //.EndChildElements()
                                           .Compose();

                weightBar = Composers["weightbar"].GetStatbar("weightstatbar");
                TryOpen();
            }
            else if (weightmod.config.HUD_POSITION == "healthstatbar")
            {
                float num = 850f;
                ElementBounds bounds1 = new ElementBounds()
                {
                    Alignment = EnumDialogArea.CenterBottom,
                    BothSizing = ElementSizing.Fixed,
                    fixedWidth = num,
                    fixedHeight = 100
                }.WithFixedAlignmentOffset(0.5, 0);

                Composers["weightbar"] = capi.Gui.CreateCompo("weight-statbar", bounds1.FlatCopy().FixedGrow(0.0, 20.0));
                ElementBounds bounds2 = ElementStdBounds.Statbar(EnumDialogArea.LeftTop, (double)num * 0.41).WithFixedAlignmentOffset(0, -0);
                bounds2.WithFixedHeight(10.0);

                ITreeAttribute treeAttribute2 = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("weightmod");

                var weightStatBar = new GuiElementStatbar(capi, bounds2, GuiStyle.XPBarColor, false, false);

                Composers["weightbar"].BeginChildElements(bounds1)
                                           .AddInteractiveElement(weightStatBar, "weightstatbar")
                                           .EndChildElements()
                                           .Compose();

                weightBar = Composers["weightbar"].GetStatbar("weightstatbar");
                TryOpen();
            }
            else
            {
                float num = 850f;
                ElementBounds bounds1 = new ElementBounds()
                {
                    Alignment = EnumDialogArea.CenterBottom,
                    BothSizing = ElementSizing.Fixed,
                    fixedWidth = num,
                    fixedHeight = 100
                }.WithFixedAlignmentOffset(0.5, 0);

                Composers["weightbar"] = capi.Gui.CreateCompo("weight-statbar", bounds1.FlatCopy().FixedGrow(0.0, 20.0));
                ElementBounds bounds2 = ElementStdBounds.Statbar(EnumDialogArea.LeftTop, (double)num * 0.41).WithFixedAlignmentOffset(0, -0);
                bounds2.WithFixedHeight(10.0);

                ITreeAttribute treeAttribute2 = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("weightmod");

                var weightStatBar = new GuiElementStatbar(capi, bounds2, GuiStyle.XPBarColor, false, false);

                Composers["weightbar"].BeginChildElements(bounds1)
                                           .AddInteractiveElement(weightStatBar, "weightstatbar")
                                           .EndChildElements()
                                           .Compose();

                weightBar = Composers["weightbar"].GetStatbar("weightstatbar");
                TryOpen();
            }




        }
        public override bool TryClose() => false;
        public override bool ShouldReceiveKeyboardEvents() => false;
        public override bool Focusable => false;
        public override void Dispose()
        {
            base.Dispose();
            if (capi.World.Player != null)
            {
                capi.World.Player.Entity.WatchedAttributes.UnregisterListener(() => UpdateWeight());
            }
        }
    }
}
