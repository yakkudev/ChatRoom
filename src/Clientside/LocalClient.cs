using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using ChatRoom.Packets;

namespace ChatRoom.Clientside;

public class LocalClient {
    volatile string username;
    volatile string session = "";
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
        Console.WriteLine($"Connecting to {host}, port {port}");
        var retries = 10;
        while (!TryConnect()) {
            if (--retries < 0) {
                Console.WriteLine($"Error establishing connection");
                return;
            }
            Console.WriteLine($"Retrying connection...");
            Thread.Sleep(1000);
        }

        Debug.Assert(tcpClient != null, nameof(tcpClient) + " != null");

        using var stream = tcpClient.GetStream();
        using var reader = new BinaryReader(stream, new UTF8Encoding(false));
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false));
        
        var receiveThread = new Thread(() => {
            byte? packetType;
            while ((packetType = reader.ReadByte()) != null) {
                switch ((PacketType)packetType) {
                    case PacketType.Ok:
                        break;
                    case PacketType.Fail:
                        Console.WriteLine($"[!] Host rejected request");
                        break;
                    case PacketType.AssignSession:
                        Console.WriteLine($"Received session token!");
                        session = Packet.ReadFrom<AssignSessionPacket>(reader).Session;
                        break;
                    case PacketType.Rename:
                        username = Packet.ReadFrom<RenamePacket>(reader).Name;
                        Console.WriteLine($"[#] Your username was set to <{username}>");
                        break;
                    case PacketType.ChatMessage: {
                        var p = Packet.ReadFrom<ChatMessagePacket>(reader);
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.WriteLine($"<{p.SenderName}> {p.Message}");
                        Console.Write("> ");
                        break;
                    }
                    default:
                        Console.WriteLine($"[!] Bad packet (#{packetType})");
                        break;
                }
            }
        });
        receiveThread.Start();

        // send connection packet to server
        new SendConnectPacket(username, Program.Version).WriteTo(writer);
        
        // block until session token received
        while (session.Length == 0) {}
        Console.WriteLine("-=-=- CONNECTED -=-=-");
        Console.Write("> ");
        
        // user input
        string? input;
        while ((input = Console.ReadLine()) != null ) {
            if (string.IsNullOrWhiteSpace(input)) {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write("> ");
                continue;
            }
            
            input = EmojiProcessor.Emojify(input);
            
            var currentPos = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine($"[{username}] {input}");
            Console.SetCursorPosition(0, currentPos);
            Console.Out.Flush();
            
            new SendChatMessagePacket(session, input).WriteTo(writer);
            Console.Write("> ");
        } 
    }
}