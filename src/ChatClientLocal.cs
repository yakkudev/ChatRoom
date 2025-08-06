using System.Net.Sockets;

namespace ChatRoom;

public class ChatClientLocal {

    string host;
    int port;

    TcpClient client;

    public ChatClientLocal(string host, int port) {
        this.host = host;
        this.port = port;

        Connect();
    }

    bool TryConnect() {
        try {
            client = new TcpClient(host, port);
        }
        catch (Exception){
            return false;
        }

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

        Console.WriteLine("###### CONNECTED ######");

        var name = "anonymous";
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream);
        writer.AutoFlush = true;
        
        var receiveThread = new Thread(() => {
            string? line;
            while ((line = reader.ReadLine()) != null) {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine(line);
                Console.Write("> ");
                Console.Out.Flush();
            }
        });
        receiveThread.Start();

        string? input;
        while ((input = Console.ReadLine()) != null) {
            int currentPos = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine($"#> {input}");
            Console.SetCursorPosition(0, currentPos);
            Console.Out.Flush();
            
            writer.WriteLine($"<{name}> {input}");
            Console.Write("> ");
        } 
    }
}