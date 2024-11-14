using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace weightmod.src
{
    public class weightmod : ModSystem
    {
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        private static Dictionary<string, float> mapLastCalculatedPlayerWeight = new Dictionary<string, float>();
        private static Dictionary<string, bool> inventoryWasModified = new Dictionary<string, bool>();
        private static Dictionary<string, float> classBonuses = new Dictionary<string, float>();
        public static Dictionary<int, float> itemIdToWeight = new Dictionary<int, float>();
        public static Dictionary<int, float> blockIdToWeight = new Dictionary<int, float>();
        public static Dictionary<int, float> itemBonusIdToWeight = new Dictionary<int, float>();
        public static string bArrIITW;
        public static string bArrBITW;
        public static string bArrIBITW;
        public static Harmony harmonyInstance;
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;
        public const string harmonyID = "weightmod.Patches";
        static weightmod instance;
        public static Config Config { get; private set; } = null!;
        public void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            var ep = new EntityBehaviorWeightable(byPlayer.Entity);
            ep.PostInit();
            byPlayer.Entity.AddBehavior(ep);
        }
        public void OnPlayerNowPlayingClient(IClientPlayer byPlayer)
        {
            //capi.World.Player
            //var ep = new EntityBehaviorWeightable(byPlayer.Entity);
            //ep.PostInit();
            //byPlayer.Entity.AddBehavior(ep);
            var ep = new EntityBehaviorWeightable(byPlayer.Entity);
            ep.PostInit();
            byPlayer.Entity.AddBehavior(ep);
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
        public static Dictionary<string, float> getlastCalculatedPlayerWeight()
        {
            return mapLastCalculatedPlayerWeight;
        }
        public static Dictionary<string, bool> getinventoryWasModified()
        {
            return inventoryWasModified;
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            classBonuses = new Dictionary<string, float>();
            api.RegisterEntityBehaviorClass("affectedByItemsWeight", typeof(EntityBehaviorWeightable));
            harmonyInstance = new Harmony(harmonyID); 
            harmonyInstance.Patch(typeof(PModuleOnGround).GetMethod("Applicable"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ApplicableOnGround")));
            harmonyInstance.Patch(typeof(PModuleInLiquid).GetMethod("Applicable"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ApplicableInLiquid")));
            harmonyInstance.Patch(typeof(PModuleInAir).GetMethod("Applicable"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_ApplicableInAir")));
        }
        public weightmod()
        {
            instance = this;
        }
        public static weightmod getInstance()
        {
            return instance;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            itemIdToWeight = new Dictionary<int, float>();
            blockIdToWeight = new Dictionary<int, float>();
            itemBonusIdToWeight = new Dictionary<int, float>();

            capi = api;
            base.StartClientSide(api);

            loadConfig(api);

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
                        item.Attributes.Token["weightmod"] = val;
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
                    }
                }

            });
            capi.Event.PlayerJoin += OnPlayerNowPlayingClient;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            mapLastCalculatedPlayerWeight = new Dictionary<string, float>();
            inventoryWasModified = new Dictionary<string, bool>();          
            itemIdToWeight = new Dictionary<int, float>();
            blockIdToWeight = new Dictionary<int, float>();
            itemBonusIdToWeight = new Dictionary<int, float>();          
            sapi = api;
            base.StartServerSide(api);

            loadConfig(api);

            loadClassBonusesMap();
            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            api.Event.PlayerDisconnect += OnPlayerDisconnect;
            api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, onServerExit);
            api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, fillWeightDictionary);
            serverChannel = sapi.Network.RegisterChannel("weightmod");
            serverChannel.RegisterMessageType(typeof(syncWeightPacket));          
            api.Event.PlayerNowPlaying += sendNewValues;
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.InventoryBase).GetMethod("DidModifyItemSlot"), postfix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_OnItemSlotModified")));
  
        }
        public void sendNewValues(IServerPlayer byPlayer)
        {
            sapi.Event.RegisterCallback((dt =>
            {
                if (byPlayer.ConnectionState == EnumClientState.Playing)
                {
                    serverChannel.SendPacket(new syncWeightPacket()
                    {
                        iITW = bArrIITW,
                        bITW = bArrBITW,
                        iBITW = bArrIBITW

                    },
                    byPlayer);                   
                }
            }
            ), 20 * 1000);                      
        }
        public void fillWeightDictionary()
        {
            string[] tmp = new string[2];
            foreach(var it in sapi.World.Items)
            {
                foreach(var it_prepared in Config.WEIGHTS_FOR_ITEMS)
                {
                    tmp = it_prepared.Key.Split(':');
                    if(it.Code != null && it.Code.Domain.Equals(tmp[0]) && it.Code.Path.Contains(tmp[1]))
                    {
                     
                        if(itemIdToWeight.ContainsKey(it.Id))
                        {
                            continue;
                        }
                        if (it.Attributes != null)
                        {
                            it.Attributes.Token["weightmod"] = it_prepared.Value;                           
                            it.Attributes = new JsonObject(it.Attributes.Token);
                            itemIdToWeight.Add(it.Id, it_prepared.Value);
                        }
                        /*else
                        {
                            JToken jt = JToken.Parse("{}");
                            jt["weightmod"] = it_prepared.Value;
                            it.Attributes = new JsonObject(jt);
                        }*/
                    }
                }              
            }
            foreach (var it in sapi.World.Blocks)
            {
                foreach (var it_prepared in Config.WEIGHTS_FOR_BLOCKS)
                {
                    tmp = it_prepared.Key.Split(':');
                    if (it.Code != null && it.Code.Domain.Equals(tmp[0]) && it.Code.Path.Contains(tmp[1]))
                    {
                        if (itemIdToWeight.ContainsKey(it.Id))
                        {
                            continue;
                        }
                        if (it.Attributes != null)
                        {
                            it.Attributes.Token["weightmod"] = it_prepared.Value;
                            it.Attributes = new JsonObject(it.Attributes.Token);
                            //itemIdToWeight.Add(it.Id, it_prepared.Value);
                        }
                        blockIdToWeight.Add(it.Id, it_prepared.Value);
                    }
                }
            }
            foreach (var it in sapi.World.Items)
            {
                foreach (var it_prepared in Config.WEIGHTS_BONUS_ITEMS)
                {
                    tmp = it_prepared.Key.Split(':');
                    if (it.Code != null && it.Code.Domain.Equals(tmp[0]) && it.Code.Path.Equals(tmp[1]))
                    {
                        if (itemIdToWeight.ContainsKey(it.Id))
                        {
                            continue;
                        }
                        if (it.Attributes != null)
                        {
                            it.Attributes.Token["weightbonusbags"] = it_prepared.Value;
                            it.Attributes = new JsonObject(it.Attributes.Token);
                            //itemIdToWeight.Add(it.Id, it_prepared.Value);
                        }
                        itemBonusIdToWeight.Add(it.Id, it_prepared.Value);
                    }
                }
            }
            foreach (var it in sapi.World.Items)
            {
                foreach (var it_prepared in Config.WEIGHTS_FOR_ENDS_WITH)
                {
                    tmp = it_prepared.Key.Split(':');
                    if (it.Code != null && it.Code.Domain.Equals(tmp[0]) && it.Code.Path.EndsWith(tmp[1]))
                    {
                        if (itemIdToWeight.ContainsKey(it.Id))
                        {
                            continue;
                        }
                        if (it.Attributes != null)
                        {
                            it.Attributes.Token["weightmod"] = it_prepared.Value;
                            it.Attributes = new JsonObject(it.Attributes.Token);
                            //itemIdToWeight.Add(it.Id, it_prepared.Value);
                        }
                        itemIdToWeight.Add(it.Id, it_prepared.Value);
                    }
                }
            }

            string tmpStr;
            tmpStr = JsonConvert.SerializeObject(itemIdToWeight, Formatting.Indented);
            bArrIITW = compressStr(tmpStr);
            tmpStr = JsonConvert.SerializeObject(blockIdToWeight, Formatting.Indented);
            bArrBITW = compressStr(tmpStr);
            tmpStr = JsonConvert.SerializeObject(itemBonusIdToWeight, Formatting.Indented);
            bArrIBITW = compressStr(tmpStr);
        }
        public string compressStr(string inStr)
        {
            byte[] compressedBytes;
            using (var uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(inStr)))
            {
                using (var compressedStream = new MemoryStream())
                {
                    // setting the leaveOpen parameter to true to ensure that compressedStream will not be closed when compressorStream is disposed
                    // this allows compressorStream to close and flush its buffers to compressedStream and guarantees that compressedStream.ToArray() can be called afterward
                    // although MSDN documentation states that ToArray() can be called on a closed MemoryStream, I don't want to rely on that very odd behavior should it ever change
                    using (var compressorStream = new DeflateStream(compressedStream, CompressionLevel.Fastest, true))
                    {
                        uncompressedStream.CopyTo(compressorStream);
                    }

                    // call compressedStream.ToArray() after the enclosing DeflateStream has closed and flushed its buffer to compressedStream
                    compressedBytes = compressedStream.ToArray();
                }
            }

            return Convert.ToBase64String(compressedBytes);
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
            foreach (string it in Config.CLASS_WEIGHT_BONUS.Split(';'))
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
                Config = api.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
                if(Config == null)
                {
                    Config = new Config();
                    api.StoreModConfig<Config>(Config, this.Mod.Info.ModID + ".json");
                    return;
                }
            }
            catch (Exception e)
            {
                Config = new Config();
            }


            
            api.StoreModConfig<Config>(Config, this.Mod.Info.ModID + ".json");
            return;
        }
        public override void Dispose()
        {
            if (harmonyInstance != null)
            {
                harmonyInstance.UnpatchAll(harmonyID);
            }
            /*classBonuses.Clear();
            itemIdToWeight.Clear();
            blockIdToWeight.Clear();
            itemBonusIdToWeight.Clear();*/

            Config = null;
            sapi = null;
            capi = null;
            mapLastCalculatedPlayerWeight = null;
            inventoryWasModified = null;
            classBonuses = null;
            itemIdToWeight = null;
            blockIdToWeight = null;
            itemBonusIdToWeight = null;
            bArrIITW = null;
            bArrBITW = null;
            bArrIBITW = null;
            harmonyInstance = null;
            serverChannel = null;
            clientChannel = null;
        }
        static readonly DateTime start = new DateTime(1970, 1, 1);
        public static long getEpochSeconds()
        {
            return (long)((DateTime.UtcNow - start).TotalSeconds);
        }
    }
}
