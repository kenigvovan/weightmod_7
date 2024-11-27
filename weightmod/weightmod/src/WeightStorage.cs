using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Newtonsoft.Json;
using Vintagestory.API.Datastructures;
using Newtonsoft.Json.Linq;

namespace weightmod.src
{
    public class WeightStorage
    {
        private ICoreAPI api;
        public string bArrIITW;
        public string bArrBITW;
        public string bArrIBITW;
        private ItemCategorizer itemCategorizer;
        Config config;
        IServerNetworkChannel serverChannel;
        public Dictionary<int, float> itemIdToWeight = new Dictionary<int, float>();
        public Dictionary<int, float> blockIdToWeight = new Dictionary<int, float>();
        public Dictionary<int, float> itemBonusIdToWeight = new Dictionary<int, float>();
        public WeightStorage(ICoreAPI api, Config config)
        {
            this.api = api;
            this.config = config;
            serverChannel = api.Network.GetChannel("weightmod") as IServerNetworkChannel;
        }

        public void ChangeCollectablesWeight()
        {
            string[] tmp = new string[2];
            foreach (var it in api.World.Items)
            {
                foreach (var it_prepared in config.WEIGHTS_FOR_ITEMS)
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
                        }
                        else
                        {
                            JToken jt = JToken.Parse("{}");
                            jt["weightmod"] = it_prepared.Value;
                            it.Attributes = new JsonObject(jt);
                        }
                        itemIdToWeight[it.Id] = it_prepared.Value;
                    }
                }
            }
            foreach (var it in api.World.Blocks)
            {
                foreach (var it_prepared in config.WEIGHTS_FOR_BLOCKS)
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
                        }
                        else
                        {
                            JToken jt = JToken.Parse("{}");
                            jt["weightmod"] = it_prepared.Value;
                            it.Attributes = new JsonObject(jt);
                        }
                        blockIdToWeight[it.Id] = it_prepared.Value;
                    }
                }
            }
            foreach (var it in api.World.Items)
            {
                foreach (var it_prepared in config.WEIGHTS_BONUS_ITEMS)
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
                        }
                        else
                        {
                            JToken jt = JToken.Parse("{}");
                            jt["weightbonusbags"] = it_prepared.Value;
                            it.Attributes = new JsonObject(jt);
                        }
                        itemBonusIdToWeight[it.Id] = it_prepared.Value;
                    }
                }
            }
            foreach (var it in api.World.Items)
            {
                foreach (var it_prepared in config.WEIGHTS_FOR_ENDS_WITH)
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
                        }
                        else
                        {
                            JToken jt = JToken.Parse("{}");
                            jt["weightmod"] = it_prepared.Value;
                            it.Attributes = new JsonObject(jt);
                        }
                        itemIdToWeight[it.Id] = it_prepared.Value;
                    }
                }
            }

            try
            {
                string tmpStr = JsonConvert.SerializeObject(itemIdToWeight, Formatting.Indented);
                bArrIITW = CompressStr(tmpStr);

                tmpStr = JsonConvert.SerializeObject(blockIdToWeight, Formatting.Indented);
                bArrBITW = CompressStr(tmpStr);

                tmpStr = JsonConvert.SerializeObject(itemBonusIdToWeight, Formatting.Indented);
                bArrIBITW = CompressStr(tmpStr);

                api.Logger.Notification($"Network sync data prepared: Items={itemIdToWeight.Count}, Blocks={blockIdToWeight.Count}, BonusItems={itemBonusIdToWeight.Count}");
            }
            catch (Exception ex)
            {
                api.Logger.Error($"Error preparing network sync: {ex.Message}");
            }

            api.Logger.Notification("Finished filling weight dictionary");
        }
        public string CompressStr(string inStr)
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
        public void sendNewValues(IServerPlayer byPlayer)
        {
            api.Event.RegisterCallback((dt =>
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


    }
}
