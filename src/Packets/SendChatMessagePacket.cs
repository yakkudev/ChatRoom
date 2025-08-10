namespace ChatRoom.Packets;

public class SendChatMessagePacket(string senderSession, string message) : Packet {
    public string Session { get; set; } = senderSession;
    public string Message { get; set; } = message;

    protected override PacketType Type => PacketType.SendChatMessage;

    public SendChatMessagePacket() : this("", "") { }
}
