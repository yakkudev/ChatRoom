namespace ChatRoom.Packets;

public class AssignSessionPacket(string session) : Packet {
    string Session { get; } = session;

    public override PacketType Type => PacketType.AssignSession;

    public override void WriteTo(BinaryWriter binaryWriter) {
        base.WriteTo(binaryWriter);
        binaryWriter.Write(Session);
    }
}