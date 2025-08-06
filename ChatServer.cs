using System.Net;
using System.Net.Sockets;

namespace ChatRoom;

public class ChatServer {
    readonly List<StreamWriter> clients = new();
    readonly Lock threadLock = new();

    readonly int port;

    public ChatServer(int port) {
        this.port = port;
        Start();
    }

    void Start() {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        Console.WriteLine($"ChatRoom server started on port {port}");

        while (true) {
            var client = listener.AcceptTcpClient();
            Console.WriteLine("new client connected.");

            var clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    void HandleClient(TcpClient client) {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream);
        writer.AutoFlush = true;

        lock (threadLock) {
            clients.Add(writer);
        }

        try {
            string? message;
            while ((message = reader.ReadLine()) != null) {
                Console.WriteLine($"receive: {message}");
                BroadcastMessage(message, writer);
            }
        }
        catch (IOException) {}

        lock (threadLock) {
            clients.Remove(writer);
        }
        
        Console.WriteLine("client dropped.");
        // todo: broadcast system message here
    }

    void BroadcastMessage(string message, StreamWriter sender) {
        lock (threadLock) {
            foreach (var client in clients) {
                if (client != sender) {
                    // todo: change StreamWriters to a "User" class or something
                    client.WriteLine(message);
                }
            }
        }
    }
}