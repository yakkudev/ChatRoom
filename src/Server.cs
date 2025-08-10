using System.Net;
using System.Net.Sockets;
using ChatRoom.Packets;

namespace ChatRoom;

public class Server {
    readonly List<Client> clients = new();
    readonly Lock threadLock = new();

    readonly int port;

    public Server(int port) {
        this.port = port;
        Start();
    }

    void Start() {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"ChatRoom server started on port {port}");

        while (true) {
            var tcpClient = listener.AcceptTcpClient();
            var client = new Client(tcpClient, Client.GenerateUsername());
            Console.WriteLine($"new client <{client.Name}> connected. Session: {client.Session}");

            var clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    void AddClient(Client client) {
        lock (threadLock) {
            clients.Add(client);
        }
        client.SendPacket(new Packets.AssignSessionPacket(client.Session));
    }

    void DropClient(Client client) {
        // todo: send drop packet just to be nice
        lock (threadLock) {
            clients.Remove(client);
        }
        Console.WriteLine($"client <{client.Name}> dropped.");
    }

    Client? GetClientByName(string name) {
        lock (threadLock) {
            return clients.FirstOrDefault(client => client.Name == name);
        }
    }
    
    void HandleClient(Client client) {
        AddClient(client);

        // todo: better packet handling (packet listener/handler methods)
        try {
            byte? packetType;
            while ((packetType= client.Reader.ReadByte()) != null) {
                switch ((PacketType)packetType) {
                    case PacketType.SendConnect: {
                        var p = Packet.ReadFrom<SendConnectPacket>(client.Reader);
                        var newName = Client.SanitizeUsername(p.Username) ?? Client.GenerateUsername();

                        // check if already exists
                        if (GetClientByName(newName) != null) {
                            Console.WriteLine($"renaming <{client.Name}> to <{newName}> failed! (already exists)");
                            client.SendPacket(new FailPacket());
                            client.SendPacket(new RenamePacket(client.Name));
                            break;
                        }

                        Console.WriteLine($"renaming <{client.Name}> to <{newName}>");
                        client.SendPacket(new RenamePacket(newName));
                        client.Name = newName;
                        break;
                    }
                    case PacketType.SendChatMessage: {
                        var p = Packet.ReadFrom<SendChatMessagePacket>(client.Reader);
                        
                        if (client.Session != p.Session) {
                            client.SendPacket(new FailPacket());
                            Console.WriteLine($"bad session: <{client.Session}, {p.Session}>");
                            break;
                        }
                        // todo: sanitize message
                        Console.WriteLine($"message: <{client.Name}> {p.Message}");
                        BroadcastPacket(client, new ChatMessagePacket(client.Name, p.Message));
                        break;
                    }
                    default:
                        Console.WriteLine($"WARN: received bad packet (#{(int)packetType}) from {client.Name}");
                        client.SendPacket(new FailPacket());
                        break;
                }
            }
        }
        catch (IOException) {}

        DropClient(client);
    }

    void BroadcastPacket(Client origin, Packet packet) {
        lock (threadLock) {
            foreach (var client in clients.Where(client => client != origin)) {
                client.SendPacket(packet);
            }
        }
    }
}