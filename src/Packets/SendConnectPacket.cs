namespace ChatRoom.Packets;

public class SendConnectPacket(string username, string version) : Packet {
    public string Username { get; set; } = username;
    public string Version { get; set; }= version;

    protected override PacketType Type => PacketType.SendConnect;

    public SendConnectPacket() : this("", "") { }
}
