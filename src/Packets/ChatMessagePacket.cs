namespace ChatRoom.Packets;

public class ChatMessagePacket(string senderName, string message) : Packet {
    string Message { get; } = message;
    string SenderName { get; } = senderName;

    public override PacketType Type => PacketType.ChatMessage;

    public override void WriteTo(BinaryWriter binaryWriter) {
        base.WriteTo(binaryWriter);
        binaryWriter.Write(SenderName);
        binaryWriter.Write(Message);
    }
}
