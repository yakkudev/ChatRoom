namespace ChatRoom.Packets;

public class SendCommandPacket (string session, string command) : Packet {
    public string Session { get; set; } = session;
    public string Command { get; set; } = command;

    protected override PacketType Type => PacketType.SendCommand;

    public SendCommandPacket() : this("", "") { }

}