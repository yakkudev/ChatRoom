namespace ChatRoom.Packets;

public class SendChatMessagePacket(string senderSession, string message) : Packet {
    string Message { get; } = message;
    string Session { get; } = senderSession;

    public override PacketType Type => PacketType.SendChatMessage;

    public override void WriteTo(BinaryWriter binaryWriter) {
        base.WriteTo(binaryWriter);
        binaryWriter.Write(Session);
        binaryWriter.Write(Message);
    }
}
