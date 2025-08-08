using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatRoom;

public class Client {
    public string Name;
    public readonly string Session;
    public readonly BinaryWriter Writer;
    public readonly BinaryReader Reader;
    public readonly TcpClient TcpClient;

    public Client(TcpClient tcpClient, string name) {
        TcpClient = tcpClient;
        Name = name;
        Session = RandomNumberGenerator.GetHexString(64);
        
        var stream = TcpClient.GetStream();
        Reader = new BinaryReader(stream, new UTF8Encoding(false));
        Writer = new BinaryWriter(stream, new UTF8Encoding(false));
    }

    public static string GenerateUsername() {
        return RandomNumberGenerator.GetHexString(6, true);
    }
    
    public static string? SanitizeUsername(string username) {
        var sanitized = Regex.Replace(username, @"[^a-zA-Z1-9_-]", "");

        if (sanitized.Length > 10)
            sanitized = sanitized[..10];
        if (sanitized.Length == 0)
            return null;

        return sanitized;
    }

    public void SendPacket(Packet packet) {
        packet.WriteTo(Writer);
    }
    
}