namespace ChatRoom.Packets;

public class OkPacket : Packet {
    public override PacketType Type => PacketType.Ok;
}