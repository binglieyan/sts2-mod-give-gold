#nullable enable

using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace GiveGold.Network.Messages;

public struct GiveGoldRequestMessage : INetMessage, IPacketSerializable
{
    public string RequestId;
    public ulong SenderId;
    public ulong TargetPlayerId;
    public int Amount;

    public readonly bool ShouldBroadcast => true;
    public readonly bool ShouldBuffer => false;

    public readonly NetTransferMode Mode => NetTransferMode.Reliable;

    public readonly LogLevel LogLevel => LogLevel.Debug;

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteString(RequestId ?? string.Empty);
        writer.WriteULong(SenderId);
        writer.WriteULong(TargetPlayerId);
        writer.WriteInt(Amount);
    }

    public void Deserialize(PacketReader reader)
    {
        RequestId = reader.ReadString();
        SenderId = reader.ReadULong();
        TargetPlayerId = reader.ReadULong();
        Amount = reader.ReadInt();
    }
}