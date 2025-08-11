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
        this.host = host;
        this.username = username;
        
        this.port = port;
        Connect();
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
            try { HandleNetwork(reader); }
            catch (Exception) {}
            finally {
                Console.WriteLine($"Connection closed.");
                tcpClient?.Close();
                Environment.Exit(0);
            }
        });
        receiveThread.Start();

        // send connection packet to server
        new SendConnectPacket(username, Program.Version).WriteTo(writer);
        
        // block until session token received
        while (session.Length == 0) {}
        
        // user input
        string? input;
        while ((input = Console.ReadLine()) != null ) {
            if (string.IsNullOrWhiteSpace(input)) {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write("> ");
                continue;
            }
            
            if (input.StartsWith('/')) {
                if (ExecuteCommand(input, writer, reader)) return;
                continue;
            }

            SendChatMessage(input, writer);
        } 
    }

    void SendChatMessage(string input, BinaryWriter writer) {
        var sourceLength = input.Length;
        var message = EmojiProcessor.Emojify(input);
        // pad message to source length
        message = message.PadRight(sourceLength, ' ');
            
        // write message to console
        var currentPos = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.WriteLine($"[{username}] {message}");
        Console.SetCursorPosition(0, currentPos);
        Console.Out.Flush();
       
        new SendChatMessagePacket(session, message).WriteTo(writer);
        Console.Write("> ");
    }

    bool ExecuteCommand(string input, BinaryWriter writer, BinaryReader reader) {
        var cmd = input[1..].Trim();
                
        // local commands
        if (cmd.StartsWith("quit") || cmd.StartsWith("exit")) {
            Console.WriteLine($"[!] Disconnecting...");
            tcpClient.Close();
            return true;
        }
        
        if (cmd.StartsWith("clear")) {
            Console.Clear();
            Console.Write("> ");
            return false;
        }
                
        // send command to server
        new SendCommandPacket(session, cmd).WriteTo(writer);
        Console.WriteLine();
        Console.Write("> ");
        return false;
    }

    void HandleNetwork(BinaryReader reader) {
        byte? packetType;
        while (tcpClient != null && tcpClient.Connected && (packetType = reader.ReadByte()) != null) {
            Console.SetCursorPosition(0, Console.CursorTop);
            switch ((PacketType)packetType) {
                case PacketType.Ok:
                    break;
                case PacketType.Fail:
                    var reason = Packet.ReadFrom<FailPacket>(reader).Reason;
                    if (reason.Length != 0)
                        reason = $"{reason}";
                    else
                        reason = "server rejected request";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[!] {reason}");
                    Console.ResetColor();
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
                    PrintMessage(p);
                    break;
                }
                default:
                    Console.WriteLine($"[!] Bad packet (#{packetType})");
                    break;
            }

            Console.Write("\n> ");
            Console.Out.Flush();
        }
    }

    void PrintMessage(ChatMessagePacket p) {
        Console.SetCursorPosition(0, Console.CursorTop);
        
        // server message
        if (p.SenderName == "$") {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(p.Message);
            Console.ResetColor();
            return;
        }
        
        // user message
        Console.Write($"<{p.SenderName}> ");
        
        // scan for @mentions, if found, highlight part of the message
        var mention = $"@{username}";
        if (p.Message.Contains(mention)) {
            var idx = p.Message.IndexOf(mention);
            if (idx >= 0) {
                Console.Write(p.Message[..idx]);
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(mention);
            Console.ResetColor();
            Console.Write(p.Message[(idx + mention.Length)..]);
        } else {
            // no @mention, just print the message
            Console.Write(p.Message);
        }
        Console.ResetColor();
    }
}