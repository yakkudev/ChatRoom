namespace ChatRoom.Packets;

public class RenamePacket(string name) : Packet {
    string Name { get; } = name;

    public override PacketType Type => PacketType.Rename;

    public override void WriteTo(BinaryWriter binaryWriter) {
        base.WriteTo(binaryWriter);
        binaryWriter.Write(Name);
    }
}