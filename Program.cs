namespace ChatRoom;

public static class Program {
    public static void Main(string[] args) {
        
        if (args.Length == 0) {
            _ = new ChatClientLocal("localhost", 21337);
            return;
        }

        if (args.ElementAtOrDefault(0) == "--client") {
            var portArg = args.ElementAtOrDefault(1) ?? "21337";
            if (!ushort.TryParse(portArg, out var port)) {
                Console.WriteLine("error: invalid port.");
                return;
            }
            
            var hostArg = args.ElementAtOrDefault(2);
            if (hostArg == null) {
                Console.WriteLine("error: host not specified");
                return;
            }
            _ = new ChatClientLocal(hostArg, port);
            return;
        }

        if (args.ElementAtOrDefault(0) == "--server") {
            var portArg = args.ElementAtOrDefault(1);
            if (portArg == null) {
                Console.WriteLine("error: no port provided.");
                return;
            }
            if (!ushort.TryParse(portArg, out var port)) {
                Console.WriteLine("error: invalid port.");
                return;
            }

            _ = new ChatServer(port);
            return;
        }
        
        Console.WriteLine("ChatRoom - client/server chat room application");
        Console.WriteLine("usage:");
        Console.WriteLine("<no args> - start client");
        Console.WriteLine("--client <port> <host> - start client with arguments");
        Console.WriteLine("--server <port> - start server at port");
    }
}