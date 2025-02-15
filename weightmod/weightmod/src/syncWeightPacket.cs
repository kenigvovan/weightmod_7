using ProtoBuf;

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
