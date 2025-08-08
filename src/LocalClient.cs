using System.Diagnostics;
using System.Net.Sockets;

namespace ChatRoom;

public class LocalClient {
    string username;
    string session = "";
    string host;
    int port;

    TcpClient? tcpClient;

    public LocalClient(string? host, int port, string? username) {
        this.host = host ?? UserPrompt("Specify hostname: ", "localhost");
        this.username = username ?? UserPrompt("Your username: ", "anon");
        
        this.port = port;
        Connect();
    }

    static string UserPrompt(string prompt = "", string defaultVal = "") {
        Console.Write(prompt);
        var name = Console.ReadLine()!;
        return string.IsNullOrWhiteSpace(name) ? defaultVal : name;
    }

    bool TryConnect() {
        try {
            tcpClient = new TcpClient(host, port);
        } catch (Exception){ return false; }
        return true;
    }

    void Connect() {
        Console.WriteLine($"connecting to {host}, port {port}");
        var retries = 10;
        while (!TryConnect()) {
            if (--retries < 0) {
                Console.WriteLine($"error: could not establish connection");
                return;
            }
            Console.WriteLine($"retrying connection...");
            Thread.Sleep(1000);
        }

        Debug.Assert(tcpClient != null, nameof(tcpClient) + " != null");

        using var stream = tcpClient.GetStream();
        using var reader = new BinaryReader(stream);
        using var writer = new BinaryWriter(stream);
        
        var receiveThread = new Thread(() => {
            byte? packetType;
            while ((packetType = reader.ReadByte()) != null) {
                switch ((PacketType)packetType) {
                    case PacketType.Ok:
                        break;
                    case PacketType.Fail:
                        Console.WriteLine($"server rejected request");
                        break;
                    case PacketType.AssignSession:
                        Console.WriteLine($"received session token!");
                        session = reader.ReadString();
                        break;
                    case PacketType.Rename:
                        username = reader.ReadString();
                        Console.WriteLine($"Your username was set to <{username}>");
                        break;
                    case PacketType.ChatMessage:
                        var senderName = reader.ReadString();
                        var message = reader.ReadString();
                        Console.WriteLine($"<{senderName}> {message}");
                        break;
                    default:
                        Console.WriteLine($"bad packet (#{packetType})");
                        break;
                }
            }
        });
        receiveThread.Start();

        // send connection packet to server
        new Packets.SendConnectPacket(username).WriteTo(writer);
        
        // block until session token received
        while (session.Length == 0) {}
        Console.WriteLine("###### CONNECTED ######");
        
        // user input
        string? input;
        while ((input = Console.ReadLine()) != null) {
            int currentPos = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine($"#> {input}");
            Console.SetCursorPosition(0, currentPos);
            Console.Out.Flush();
            
            new Packets.SendChatMessagePacket(session, input).WriteTo(writer);
            Console.Write("> ");
        } 
    }
}