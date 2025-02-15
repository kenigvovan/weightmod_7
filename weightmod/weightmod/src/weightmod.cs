using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using weightmod.src.EB;
using weightmod.src.gui;
using weightmod.src.harmony;

namespace weightmod.src
{
    public class weightmod : ModSystem
    {
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        public static bool clientBehaviorInit = false;
        private static Dictionary<string, float> classBonuses = new Dictionary<string, float>();
        WeightStorage weightStorage;
        WeightOracle weightOracle;

        public static Harmony harmonyInstance;
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        public const string harmonyID = "weightmod.Patches";
        static weightmod instance;
        public static Config config { get; private set; } = null!;
        public void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            var ep = new EntityBehaviorWeightable(byPlayer.Entity);
            ep.PostInit();
            byPlayer.Entity.AddBehavior(ep);
        }
        public void OnPlayerNowPlayingClient(IClientPlayer byPlayer)
        {
            if(byPlayer == null || byPlayer.Entity == null)
            {
                weightmod.capi.Event.RegisterCallback((dt =>
                {
                    var pl = weightmod.capi.World.Player;
                    var ep = new EntityBehaviorWeightable(pl.Entity);
                    ep.PostInit();
                    byPlayer.Entity.AddBehavior(ep);
                    weightmod.clientBehaviorInit = true;
                }
            ), 60 * 1000);
            }
            else
            {
                if (weightmod.capi.World.Player.PlayerUID == byPlayer.Entity.PlayerUID)
                {
                    var ep = new EntityBehaviorWeightable(byPlayer.Entity);
                    ep.PostInit();
                    byPlayer.Entity.AddBehavior(ep);
                    weightmod.clientBehaviorInit = true;
                }
            }
           
        }
        public void OnPlayerDisconnect(IServerPlayer byPlayer)
        {
            if(byPlayer.Entity.HasBehavior<EntityBehaviorWeightable>())
            {
                byPlayer.Entity.RemoveBehavior(byPlayer.Entity.GetBehavior<EntityBehaviorWeightable>());
            }
        }
        public static Dictionary<string, float> getclassBonuses()
        {
            return classBonuses;
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            classBonuses = new Dictionary<string, float>();
            api.RegisterEntityBehaviorClass("affectedByItemsWeight", typeof(EntityBehaviorWeightable));
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntityInAir).GetMethod("Applicable"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ApplicableInAir")));
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntityInLiquid).GetMethod("Applicable"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ApplicableInLiquid")));
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.EntityOnGround).GetMethod("Applicable"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ApplicableOnGround")));
        }
        public weightmod()
        {
            instance = this;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            weightmod.clientBehaviorInit = false;
            capi = api;
            loadConfig(api);
            EntityBehaviorWeightable.config = config;

            api.Gui.RegisterDialog((GuiDialog)new HudWeightPlayer((ICoreClientAPI)api));
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.CollectibleObject).GetMethod("GetHeldItemInfo"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Postfix_GetHeldItemInfo")));
            clientChannel = api.Network.RegisterChannel("weightmod");
            clientChannel.RegisterMessageType(typeof(syncWeightPacket));
            clientChannel.SetMessageHandler<syncWeightPacket>((packet) =>
            {
                
                Dictionary<int, float> tmpDict = JsonConvert.DeserializeObject<Dictionary<int,float>>(Decompress(packet.iITW));

                foreach (var item in capi.World.Items)
                {
                    if (tmpDict.TryGetValue(item.Id, out float val))
                    {
                        if (item.Attributes != null)
                        {
                            item.Attributes.Token["weightmod"] = val;
                        }
                        else
                        {
                            JToken jt = JToken.Parse("{}");
                            jt["weightmod"] = val;
                            item.Attributes = new JsonObject(jt);
                        }
                    }
                }
                tmpDict = JsonConvert.DeserializeObject<Dictionary<int, float>>(Decompress(packet.bITW));
                foreach (var item in capi.World.Blocks)
                {
                    if (tmpDict.TryGetValue(item.Id, out float val))
                    {
                        if (item.Attributes != null)
                        {
                            item.Attributes.Token["weightmod"] = val;
                        }
                        else
                        {
                            JToken jt = JToken.Parse("{}");
                            jt["weightmod"] = val;
                            item.Attributes = new JsonObject(jt);
                        }
                    }
                }
                tmpDict = JsonConvert.DeserializeObject<Dictionary<int, float>>(Decompress(packet.iBITW));
                foreach (var item in capi.World.Items)
                {
                    if (tmpDict.TryGetValue(item.Id, out float val))
                    {
                        if (item.Attributes != null)
                        {
                            item.Attributes.Token["weightbonusbags"] = val;
                        }
                        else
                        {
                            JToken jt = JToken.Parse("{}");
                            jt["weightbonusbags"] = val;
                            item.Attributes = new JsonObject(jt);
                        }
                    }
                }

            });
            capi.Event.PlayerJoin += OnPlayerNowPlayingClient;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {                           
            sapi = api;
            base.StartServerSide(api);

            loadConfig(api);
            EntityBehaviorWeightable.config = config;
            serverChannel = sapi.Network.RegisterChannel("weightmod");

            weightStorage = new WeightStorage(api, config);
            weightOracle = new WeightOracle(api, config);

            loadClassBonusesMap();
            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            api.Event.PlayerDisconnect += OnPlayerDisconnect;
            api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, onServerExit);
            api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, FillDictAndSetWeight);
            
            serverChannel.RegisterMessageType(typeof(syncWeightPacket));          
            api.Event.PlayerNowPlaying += weightStorage.sendNewValues;
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.InventoryBase).GetMethod("DidModifyItemSlot"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_OnItemSlotModified"))); 
            EntityBehaviorWeightable.weightStorage = weightStorage;
        }


        private void FillDictAndSetWeight()
        {
            if (weightmod.config.USE_WEIGHT_ORACLE && !weightmod.config.WEIGHT_ORACLE_DONE)
            {
                weightOracle.FillConfigDicts();
            }
            weightStorage.ChangeCollectablesWeight();
        }    
        public static string Decompress(string compressedString)
        {
            byte[] decompressedBytes;

            var compressedStream = new MemoryStream(Convert.FromBase64String(compressedString));

            using (var decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                using (var decompressedStream = new MemoryStream())
                {
                    decompressorStream.CopyTo(decompressedStream);

                    decompressedBytes = decompressedStream.ToArray();
                }
            }

            return Encoding.UTF8.GetString(decompressedBytes);
        }
        public void onServerExit()
        {
            //classBonuses.Clear();
        }
        public static void loadClassBonusesMap()
        {
            foreach (string it in config.CLASS_WEIGHT_BONUS.Split(';'))
            {
                if (it.Length != 0)
                {
                    string[] tmp = it.Split(':');
                    classBonuses.Add(tmp[0], float.Parse(tmp[1]));
                }
            }
        }
        public void loadConfig(ICoreAPI api)
        {
            try
            {
                config = api.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
                if(config == null)
                {
                    config = new();
                    api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
                    return;
                }
            }
            catch (Exception e)
            {
                config = new Config();
            }

            api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
            return;
        }
        public override void Dispose()
        {
            if (harmonyInstance != null)
            {
                harmonyInstance.UnpatchAll(harmonyID);
            }

            config = null;
            sapi = null;
            capi = null;
            classBonuses = null;
            harmonyInstance = null;
            serverChannel = null;
            clientChannel = null;
            EntityBehaviorWeightable.config = null;
            EntityBehaviorWeightable.weightStorage = null;
            weightmod.clientBehaviorInit = false;
        }
        static readonly DateTime start = new DateTime(1970, 1, 1);
        public static long getEpochSeconds()
        {
            return (long)((DateTime.UtcNow - start).TotalSeconds);
        }
    }
}
