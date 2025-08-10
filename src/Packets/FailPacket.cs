namespace ChatRoom.Packets;

// todo: fail reasons as enum
public class FailPacket : Packet {
    protected override PacketType Type => PacketType.Fail;
}