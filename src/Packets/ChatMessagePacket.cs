namespace ChatRoom.Packets;

public class ChatMessagePacket(string senderName, string message) : Packet {
    public string SenderName { get; set; } = senderName;
    public string Message { get; set; } = message;

    protected override PacketType Type => PacketType.ChatMessage;
    public ChatMessagePacket() : this("", "") { }
}
