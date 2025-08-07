namespace ChatRoom.Packets;

// todo: fail reasons as enum
public class FailPacket : Packet {
    public override PacketType Type => PacketType.Fail;
}