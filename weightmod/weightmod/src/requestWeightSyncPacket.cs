using ProtoBuf;

namespace weightmod.src
{
    // Client -> server marker: "HUD is ready, my WatchedAttributes listener is
    // attached, push me the current weightmod tree." Server replies by calling
    // MarkPathDirty (plus shouldRecalc=true to refresh if stale).
    [ProtoContract]
    internal class requestWeightSyncPacket
    {
    }
}
