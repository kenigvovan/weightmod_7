using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace weightmod.src
{
    [ProtoContract]
    internal class syncWeightPacket
    {
        [ProtoMember(1)]
        public string iITW;
        [ProtoMember(2)]
        public string bITW;
        [ProtoMember(3)]
        public string iBITW;
    }
}
