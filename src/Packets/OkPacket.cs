namespace ChatRoom.Packets;

public class OkPacket : Packet {
    protected override PacketType Type => PacketType.Ok;
}