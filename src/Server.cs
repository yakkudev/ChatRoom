using System.Net;
using System.Net.Sockets;
using ChatRoom.Packets;

namespace ChatRoom;

public partial class Server {
    readonly List<Client> clients = new();
    readonly Lock threadLock = new();

    readonly int port;
    
    public Server(int port) {
        this.port = port;
        
        Console.WriteLine($"Privileged users: {string.Join(", ", Program.Options.PrivilegedUsers)}");
        Console.WriteLine(Program.Options.PrivilegeOnlyLocal
            ? "Privileges are only granted to local users."
            : "Privileges are granted to all users in the privileged list.");


        InitCommands();
        Start();
    }

    void Start() {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"ChatRoom server started on port {port}");

        while (true) {
            var tcpClient = listener.AcceptTcpClient();
            var client = new Client(tcpClient, Client.GenerateUsername());
            Console.WriteLine($"new client from {tcpClient.Client.RemoteEndPoint}, <{client.Name}> connected. Session: {client.Session}");

            var clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    void AddClient(Client client) {
        lock (threadLock) {
            clients.Add(client);
        }
        client.SendPacket(new AssignSessionPacket(client.Session));
        client.SendPacket(new OkPacket());
        Console.WriteLine($"client <{client.Name}> added to client list.");
    }

    void DropClient(Client client) {
        if (client.TcpClient.Connected) {
            client.TcpClient.Close();
        }
        
        lock (threadLock) {
            if (!clients.Contains(client)) {
                Console.WriteLine($"client <{client.Name}> not found in client list, cannot drop.");
                return;
            }
            
            clients.Remove(client);
        }
        Console.WriteLine($"client <{client.Name}> dropped.");
        if (client.Announced)
            BroadcastPacket(new ChatMessagePacket("$", $"User <{client.Name}> left."));
    }

    Client? GetClientByName(string name) {
        lock (threadLock) {
            return clients.FirstOrDefault(client => client.Name == name);
        }
    }
    
    void HandleClient(Client client) {
        AddClient(client);

        try {
            byte? packetType;
            while ((packetType= client.Reader.ReadByte()) != null) {
                switch ((PacketType)packetType) {
                    case PacketType.SendConnect:
                        HandleSendConnect(client, Packet.ReadFrom<SendConnectPacket>(client.Reader));
                        break;
                    case PacketType.SendChatMessage:
                        HandleSendChatMessage(client, Packet.ReadFrom<SendChatMessagePacket>(client.Reader));
                        break;
                    case PacketType.SendCommand:
                        HandleSendCommand(client, Packet.ReadFrom<SendCommandPacket>(client.Reader));
                        break;
                    default:
                        Console.WriteLine($"WARN: received bad packet (#{(int)packetType}) from {client.Name}");
                        client.SendPacket(new FailPacket());
                        break;
                }
            }
        }
        catch (Exception) {}

        DropClient(client);
    }

    bool HandleSendCommand(Client client, SendCommandPacket packet) {
        // check session
        if (client.Session != packet.Session) {
            client.SendPacket(new FailPacket("bad session"));
            Console.WriteLine($"WARN: bad session: <{client.Session}, {packet.Session}>");
            return false;
        }
        
        // handle commands
        var args = packet.Command.Split(' ');
        var command = args[0].ToLowerInvariant();
        
        if (commands.TryGetValue(command, out var cmd)) {
            if (client.Privileged || !cmd.RequiresPrivilege) {
                if (args.Length < cmd.MinArgs + 1) {
                    client.SendPacket(new FailPacket($"usage: {cmd.Usage}"));
                    return true;
                }
            } else {
                client.SendPacket(new FailPacket("no privileges"));
                Console.WriteLine($"<{client.Name}> tried to use {cmd.Name} without privileges");
                return true;
            }
            cmd.Execute(client, args);
            return true;
        }
        client.SendPacket(new FailPacket("unknown command"));
        Console.WriteLine($"unknown command: {command}");
        return true;
    }


    bool HandleSendConnect(Client client, SendConnectPacket packet) {
        if (Program.Options.StrictVersion && packet.Version != Program.Version ) {
            Console.WriteLine($"WARN: client <{client.Name}> tried to connect with wrong version: {packet.Version} (expected {Program.Version})");
            client.SendPacket(new FailPacket("wrong version"));
            DropClient(client);
            return false;
        }
        
        var newName = Client.SanitizeUsername(packet.Username) ?? Client.GenerateUsername();

        // check if client with this name already exists
        if (GetClientByName(newName) != null) {
            Console.WriteLine($"WARN: renaming <{client.Name}> to <{newName}> failed! (already exists)");
            client.SendPacket(new FailPacket());
            client.SendPacket(new RenamePacket(client.Name));
            return false;
        }

        Console.WriteLine($"renaming <{client.Name}> to <{newName}>");
        client.SendPacket(new RenamePacket(newName));
        client.Name = newName;
        
        // properly announce join
        client.Announced = true;
        BroadcastPacket(new ChatMessagePacket("$", $"User <{client.Name}> joined."), client);

        var privilegedCondition = Program.Options.PrivilegedUsers.Contains(client.Name) && (!Program.Options.PrivilegeOnlyLocal || client.IsLocal);
        if (privilegedCondition) {
            client.Privileged = true;
            client.SendPacket(new ChatMessagePacket("$", "Connected with admin privileges :) Type /help for commands."));
        } else {
            client.SendPacket(new ChatMessagePacket("$", "Connected. Type /help for commands."));
        }
        return true;
    }
    
    bool HandleSendChatMessage(Client client, SendChatMessagePacket packet) {
        // check session
        if (client.Session != packet.Session) {
            client.SendPacket(new FailPacket());
            Console.WriteLine($"bad session: <{client.Session}, {packet.Session}>");
            return false;
        }
        
        // todo: sanitize message
        Console.WriteLine($"message: <{client.Name}> {packet.Message}");
        BroadcastPacket(new ChatMessagePacket(client.Name, packet.Message));
        return true;
    }

    void BroadcastPacket(Packet packet, Client? skip = null) {
        lock (threadLock) {
            foreach (var client in clients.Where(client => client != skip)) {
                client.SendPacket(packet);
            }
        }
    }
}