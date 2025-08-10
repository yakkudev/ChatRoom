namespace ChatRoom.Packets;

public class AssignSessionPacket(string session) : Packet {
    public string Session { get; set; } = session;

    protected override PacketType Type => PacketType.AssignSession;
    public AssignSessionPacket() : this("") { }
}