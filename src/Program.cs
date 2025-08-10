using System.Reflection;

namespace ChatRoom;

public static class Program {
    public record struct ProgramOptions(
        bool DoShowHelp,
        bool DoStartServer,
        bool DoStartClient,
        string? Username,
        string? Host,
        int Port,
        List<string> PrivilegedUsers,
        bool PrivilegeOnlyLocal
    );

    public static string Version { get; private set; } = "";
    public static ProgramOptions Options { get; private set; } = new ProgramOptions(
        DoShowHelp: false,
        DoStartServer: false,
        DoStartClient: true,
        Username: null,
        Host: null,
        Port: 21337,
        PrivilegedUsers: [],
        PrivilegeOnlyLocal: false
    );

    public static void Main(string[] args) {
        Setup();

        try {
            ParseArgs(args);
        } catch (Exception e) {
            Console.WriteLine($"argument error: {e.Message}");
            return;
        }
        
        if (Options.DoStartClient && Options.DoStartServer) {
            Console.WriteLine("Cannot specify both --client and --server");
            return;
        }

        if (Options.DoShowHelp) {
            ShowHelp();
            return;
        }

        if (Options.DoStartClient) {
            _ = new Clientside.LocalClient(Options.Host, Options.Port, Options.Username);
            return;
        }

        if (Options.DoStartServer) {
            _ = new Server(Options.Port);
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
        Console.WriteLine("    --privilege <usernames>     Set privileged users (server only)");
        Console.WriteLine("    --privilege-only-local      Give privilege only to local users. Specify users using the --privilege flag (server only)");
        Console.WriteLine("    --help                      Show help");
        Console.WriteLine("");
        Console.WriteLine("Examples:");
        Console.WriteLine("    ChatRoom --user Alice");
        Console.WriteLine("    ChatRoom --client 10.0.0.5 21337 --user Sk8brd");
        Console.WriteLine("    ChatRoom --server 73312");
    }

    static void ParseArgs(string[] args) {
        var options = Options;
        
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
                
                case "--privilege":
                    if (argQueue.Count == 0) throw new ArgumentException("--privilege requires at least one username");
                    var privilegedUsers = new List<string>();
                    while (argQueue.Count > 0 && !argQueue.Peek().StartsWith("--")) {
                        privilegedUsers.Add(argQueue.Dequeue());
                    }
                    options.PrivilegedUsers.AddRange(privilegedUsers);
                    break;
                case "--privilege-only-local":
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

        Options = options;
    }
}