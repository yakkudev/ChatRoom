using System.Reflection;

namespace ChatRoom;

public static class Program {
    record struct ProgramOptions(
        bool DoShowHelp,
        bool DoStartServer,
        bool DoStartClient,
        string? Username,
        string? Host,
        int Port
    );

    public static string Version { get; private set; } = "";

    public static void Main(string[] args) {
        Setup();

        ProgramOptions options;
        try {
            options = ParseArgs(args);
        } catch (Exception e) {
            Console.WriteLine($"argument error: {e.Message}");
            return;
        }
        
        if (options.DoStartClient && options.DoStartServer) {
            Console.WriteLine("Cannot specify both --client and --server");
            return;
        }

        if (options.DoShowHelp) {
            ShowHelp();
            return;
        }

        if (options.DoStartClient) {
            _ = new Clientside.LocalClient(options.Host, options.Port, options.Username);
            return;
        }

        if (options.DoStartServer) {
            _ = new Server(options.Port);
            return;
        }
    }

    static void Setup() {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        
        Version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown"; 
        
        Console.WriteLine($"ChatRoom version {Version}");
    }

    static void ShowHelp() {
        Console.WriteLine("ChatRoom - client/server chat room application");
        Console.WriteLine("Usage:");
        Console.WriteLine("    --client [host] [port]      Start client with optional host and port");
        Console.WriteLine("    --server <port>             Start server on port");
        Console.WriteLine("    --user <username>           Set username for client");
        Console.WriteLine("    --help                      Show help");
        Console.WriteLine("");
        Console.WriteLine("Examples:");
        Console.WriteLine("    ChatRoom --user Alice");
        Console.WriteLine("    ChatRoom --client 10.0.0.5 21337 --user Sk8brd");
        Console.WriteLine("    ChatRoom --server 73312");
    }

    static ProgramOptions ParseArgs(string[] args) {
        var options = new ProgramOptions(
            DoShowHelp: false,
            DoStartServer: false,
            DoStartClient: true,
            Username: null,
            Host: null,
            Port: 21337
        );
        
        var argQueue = new Queue<string>(args); 
        while (argQueue.Count > 0) {
            var arg = argQueue.Dequeue();

            switch (arg) {
                case "--help":
                    options.DoShowHelp = true;
                    break;

                case "--user":
                    if (argQueue.Count == 0) throw new ArgumentException("--user requires a username");
                    options.Username = argQueue.Dequeue();
                    break;

                case "--client":
                    if (argQueue.Count >= 1) {
                        options.Host = argQueue.Dequeue();
                        if (argQueue.Count >= 1 && int.TryParse(argQueue.Dequeue(), out var clientPort)) {
                            options.Port = clientPort;
                        }
                    }
                    options.DoStartClient = true;
                    break;

                case "--server":
                    if (argQueue.Count == 0) throw new ArgumentException("--server requires a port specified");
                    if (!int.TryParse(argQueue.Dequeue(), out var serverPort)) throw new ArgumentException("Invalid port for --server");
                    options.Port = serverPort;
                    options.DoStartServer = true;
                    options.DoStartClient = false;
                    break;

                default:
                    throw new ArgumentException($"unknown argument: {arg}");
            }
        } 
        
        return options;
    }
}