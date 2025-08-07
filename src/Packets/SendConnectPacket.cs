namespace ChatRoom.Packets;

public class SendConnectPacket(string name) : Packet {
    string Name { get; } = name;

    public override PacketType Type => PacketType.SendConnect;

    public override void WriteTo(BinaryWriter binaryWriter) {
        base.WriteTo(binaryWriter);
        binaryWriter.Write(Name);
    }
}
