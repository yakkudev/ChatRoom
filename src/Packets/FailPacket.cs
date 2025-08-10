namespace ChatRoom.Packets;

public class FailPacket (string reason) : Packet {
    public string Reason { get; set; } = reason;
    protected override PacketType Type => PacketType.Fail;
    
    public FailPacket() : this("") { }
}