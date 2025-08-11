using System.Net.Sockets;
using System.Text;
using ChatRoom.Packets;

namespace ChatRoom.Clientside;

public class ClientNetwork {
    public event Action<AssignSessionPacket>? OnSessionAssigned;
    public event Action<RenamePacket>? OnUsernameChanged;
    public event Action<ChatMessagePacket>? OnChatMessageReceived;
    public event Action<FailPacket>? OnErrorReceived;
    public event Action? OnConnectionLost;
    public event Action? OnConnectionSuccess;
 
    public readonly string Host;
    public readonly int Port;
    
    TcpClient? tcpClient;
    BinaryReader? reader;
    BinaryWriter? writer;
    Thread? receiveThread;
    
    public string Session { get; private set; } = "";
    public bool IsConnected => tcpClient?.Connected ?? false;

    public ClientNetwork(string host, int port) {
        Host = host;
        Port = port;
    }

    public bool Connect(string username) {
        var retries = 10;
        while (retries-- > 0) {
            try {
                tcpClient = new TcpClient(Host, Port);
                
                // connection successful
                // set up streams and listener thread
                var stream = tcpClient.GetStream();
                reader = new BinaryReader(stream, new UTF8Encoding(false));
                writer = new BinaryWriter(stream, new UTF8Encoding(false));

                receiveThread = new Thread(() => HandleNetwork(reader));
                receiveThread.Start();
                
                // send the connection packet
                new SendConnectPacket(username, Program.Version).WriteTo(writer);
                OnConnectionSuccess?.Invoke();
                return true;

            } catch (SocketException) {
                // sleep before retrying
                Thread.Sleep(1000);
            }
        }
        return false;
    }
    
    public void Disconnect() {
        tcpClient?.Close();
    }

    public void SendChatMessage(string message) {
        if (!IsConnected || writer == null) return;
        new SendChatMessagePacket(Session, message).WriteTo(writer);
    }

    public void SendCommand(string command) {
        if (!IsConnected || writer == null) return;
        new SendCommandPacket(Session, command).WriteTo(writer);
    }

    void HandleNetwork(BinaryReader reader) {
        try {
            while (IsConnected) {
                var packetType = (PacketType)reader.ReadByte();
                switch (packetType) {
                    case PacketType.Ok:
                        break;
                    case PacketType.Fail:
                        OnErrorReceived?.Invoke(Packet.ReadFrom<FailPacket>(reader));
                        break;
                    case PacketType.AssignSession: {
                        var p = Packet.ReadFrom<AssignSessionPacket>(reader);
                        Session = p.Session;
                        OnSessionAssigned?.Invoke(p);
                        break;
                    }
                    case PacketType.Rename:
                        OnUsernameChanged?.Invoke(Packet.ReadFrom<RenamePacket>(reader));
                        break;
                    case PacketType.ChatMessage:
                        OnChatMessageReceived?.Invoke(Packet.ReadFrom<ChatMessagePacket>(reader));
                        break;
                    default:
                        OnErrorReceived?.Invoke(new FailPacket($"Received bad packet (#{(int)packetType})"));
                        break;
                }
            }
        }
        catch (IOException) { /* disconnect */ }
        catch (Exception e) {
            OnErrorReceived?.Invoke(new FailPacket($"Network error occured {e.Message}"));
        } finally {
            OnConnectionLost?.Invoke();
            tcpClient?.Close();
        }
    } 
}