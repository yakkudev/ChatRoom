namespace ChatRoom.Packets;

public class RenamePacket(string name) : Packet {
    public string Name { get; set; } = name;

    protected override PacketType Type => PacketType.Rename;

    public RenamePacket() : this("") { }
}