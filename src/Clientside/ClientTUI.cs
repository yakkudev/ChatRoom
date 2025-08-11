using ChatRoom.Packets;

namespace ChatRoom.Clientside;

public class ClientTUI {
    readonly ClientNetwork network;
    volatile string username;
    bool exitRequested;

    public ClientTUI(string host, int port, string username) {
        this.username = username;
        network = new ClientNetwork(host, port);
        
        SubscribeToNetworkEvents();
    }

    public void Run() {
        Console.WriteLine($"Connecting to {network.Host}, port {network.Port} as {username}...");
        
        if (!network.Connect(username)) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to establish a connection with the server.");
            Console.ResetColor();
            return;
        }

        // block until got session or lost connection
        while (string.IsNullOrEmpty(network.Session) && network.IsConnected) {
            Thread.Sleep(100);
        }
        
        // got session but disconnected (?)
        if (!network.IsConnected) return;

        // start the main loop for user input
        InputLoop();
    }

    void SubscribeToNetworkEvents() {
        network.OnConnectionSuccess += () => Console.WriteLine("Connection established.");
        network.OnSessionAssigned += (p) => Console.WriteLine("Received session token!");
        network.OnUsernameChanged += (p) => {
            username = p.Name;
            Console.WriteLine($"\n[#] Your username was set to <{username}>");
            PrintPrompt();
        };
        network.OnChatMessageReceived += PrintChatMessage;
        network.OnErrorReceived += PrintError;
        network.OnConnectionLost += () => {
            Console.WriteLine("\nConnection lost.");
            exitRequested = true;
        };
    }

    void InputLoop() {
        string? input;
        PrintPrompt();
        while (!exitRequested && (input = Console.ReadLine()) != null) {
            if (string.IsNullOrWhiteSpace(input)) {
                PrintPrompt();
                continue;
            }

            if (input.StartsWith('/')) {
                HandleCommand(input);
            } else {
                network.SendChatMessage(input);
            }
            if(!exitRequested) PrintPrompt();
        }
    }

    void HandleCommand(string input) {
        var command = input[1..].Trim();

        // local commands
        if (command.Equals("quit", StringComparison.OrdinalIgnoreCase) || command.Equals("exit", StringComparison.OrdinalIgnoreCase)) {
            Console.WriteLine("[!] Disconnecting...");
            network.Disconnect();
            exitRequested = true;
            return;
        }

        if (command.Equals("clear", StringComparison.OrdinalIgnoreCase)) {
            Console.Clear();
            return;
        }

        // server commands
        network.SendCommand(command);
    }
    
    void PrintChatMessage(ChatMessagePacket p) {
        // server message
        if (string.IsNullOrEmpty(p.SenderName) || p.SenderName == "$") {
            PrintServerMessage(p.Message);
            return;
        }
        
        ClearCurrentConsoleLine();
        
        if (p.SenderName == username) {
            Console.ForegroundColor = ConsoleColor.Cyan;
        }
        Console.Write($"<{p.SenderName}> ");
        Console.ResetColor();

        // scan for @mentions and highlight them
        var mention = $"@{username}";
        if (p.Message.Contains(mention, StringComparison.OrdinalIgnoreCase)) {
            var idx = p.Message.IndexOf(mention, StringComparison.OrdinalIgnoreCase);
            Console.Write(p.Message[..idx]);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(p.Message.Substring(idx, mention.Length));
            Console.ResetColor();
            Console.WriteLine(p.Message[(idx + mention.Length)..]);
        } else {
            Console.WriteLine(p.Message);
        }
        PrintPrompt();
    }

    static void PrintServerMessage(string message) {
        ClearCurrentConsoleLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
        PrintPrompt(); 
    }

    void PrintError(FailPacket packet) {
        ClearCurrentConsoleLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[!] {packet.Reason}");
        Console.ResetColor();
        PrintPrompt();
    }

    static void ClearCurrentConsoleLine() {
        var cursorTop = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth)); 
        Console.SetCursorPosition(0, cursorTop);
    }

    static void PrintPrompt() {
        Console.Write("> ");
    }
}
