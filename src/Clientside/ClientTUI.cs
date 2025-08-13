using System.Text;
using ChatRoom.Packets;

namespace ChatRoom.Clientside;

public class ClientTUI {
    readonly List<List<string>> formattedHistory = new();
    readonly List<string> realHistory = new();
    
    readonly ClientNetwork network;
    volatile string username;
    volatile bool newMessages;
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
        
        PrintWindow();
        Thread.Sleep(100); 

        InputLoop();
    }


    static void PrintWindow() {
        Terminal.Clear();
        Terminal.CursorStart();
        Terminal.Write(Terminal.Color.Inverse, new string('=', Terminal.Width), Terminal.Color.Reset);
        Terminal.CursorNewLine();
        for (var i = 0; i < Terminal.Height - 4; i++) {
            Terminal.CursorNewLine();
        }
        Terminal.Write(Terminal.Color.Inverse, new string('=', Terminal.Width), Terminal.Color.Reset);
        
        Terminal.Flush();
    }
    
    void RedrawMessages() {
        var drawStart = Terminal.Height - 3;
        Terminal.CursorTo(0, drawStart);
        // flatten the message history
        var flattened = formattedHistory.SelectMany(x => x).ToList(); 
        var iterCount = flattened.Count - 1;
        for (var i = iterCount; i >= 0 && (iterCount - i) < drawStart - 1; i--) {
            Terminal.ClearLine();
            Terminal.CursorForward(2);
            Terminal.Write(flattened[i]);
            Terminal.CursorLastLine();
        }
    }

    static List<string> SplitMessage(string message) {
        var lines = new List<string>();
        var words = message.Split(' ');
        var currentLine = new StringBuilder();
        var maxWidth = Terminal.Width - 2;

        foreach (var word in words) {
            // split word if too long
            if (word.Length > maxWidth) {
                // finish current line
                if (currentLine.Length > 0) {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }

                // split the word into chunks
                for (var i = 0; i < word.Length; i += maxWidth) {
                    var chunkLength = Math.Min(maxWidth, word.Length - i);
                    lines.Add(word.Substring(i, chunkLength));
                }

                // next word
                continue;
            }

            // create new line if adding this word overflows the current line
            if (currentLine.Length + word.Length + (currentLine.Length > 0 ? 1 : 0) > maxWidth) {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }

            // add a space after every word except the first one
            if (currentLine.Length > 0)
                currentLine.Append(' ');

            currentLine.Append(word);
        }

        // add the last line
        if (currentLine.Length > 0) 
            lines.Add(currentLine.ToString());

        return lines;
    }
    
    static List<string> NewlineSplitMessage(string message) {
        var lines = message.Split('\n')
            .SelectMany(SplitMessage).ToList();
        
        return lines;
    }
    
    void NewMessageFormat(string message) {
        var lines = NewlineSplitMessage(message);
        formattedHistory.Add(lines);
        newMessages = true;
    }
    
    void NewMessage(string message) {
        realHistory.Add(message);
        NewMessageFormat(message);
    }

    void SubscribeToNetworkEvents() {
        network.OnConnectionSuccess += () => NewMessage("Connection established.");
        network.OnSessionAssigned += (p) => NewMessage("Received session token!");
        network.OnUsernameChanged += (p) => {
            username = p.Name;
            PrintServerMessage("Your username was set to <" + username + ">");
        };
        network.OnChatMessageReceived += PrintChatMessage;
        network.OnErrorReceived += PrintError;
        network.OnConnectionLost += () => {
            PrintError(new FailPacket("Connection lost."));
            exitRequested = true;
        };
    }
    
    void InputLoop() {
        const string prompt = "> ";
        string? input = "";
        var (lastWidth, lastHeight) = (Terminal.Width, Terminal.Height);
        while (!exitRequested) {
            // terminal resize
            bool widthChanged;
            if ((widthChanged = Terminal.Width != lastWidth) || Terminal.Height != lastHeight) {
                lastWidth = Terminal.Width;
                lastHeight = Terminal.Height;
                PrintWindow();
                newMessages = true;
                // have to split messages again if terminal width changed
                if (widthChanged) {
                    formattedHistory.Clear();
                    foreach (var message in realHistory) {
                        NewMessageFormat(message);
                    }
                }
            }
            
            if (newMessages) {
                RedrawMessages();
                newMessages = false;
                Terminal.CursorTo(prompt.Length + 1 + input.Length, Terminal.Height - 1);
                Terminal.Flush();
            }
            
            if (Console.KeyAvailable) {
                Terminal.CursorTo(0, Terminal.Height - 1);
                var key = Console.ReadKey(true);
                input = Input(key, input);
                Terminal.ClearLine();
                Terminal.Write(prompt, input);
                Terminal.Flush();
            }
            
            Thread.Sleep(10);
        }
    }

    string Input(ConsoleKeyInfo key, string input) {
        switch (key.Key) {
            case ConsoleKey.Enter: {
                HandleChatInput(input);
                input = "";
                break;
            }
            case ConsoleKey.Backspace: {
                if (input.Length > 0) {
                    input = input[..^1]; // remove last character
                }

                break;
            }
            default: {
                if (key.KeyChar != 0) {
                    if (char.IsControl(key.KeyChar)) return input; // ignore control characters
                    if (input.Length >= Terminal.Width - 2) return input; // limit input length to terminal width
                    input += key.KeyChar;
                }

                break;
            }
        }

        return input;
    }
    
    void HandleChatInput(string input) {
        if (string.IsNullOrWhiteSpace(input)) return;

        // handle commands
        if (input.StartsWith('/')) {
            HandleCommand(input);
            return;
        }

        // send chat message
        input = EmojiProcessor.Emojify(input);
        network.SendChatMessage(input);
    }

    void HandleCommand(string input) {
        var command = input[1..].Trim();

        // local commands
        if (command.Equals("quit", StringComparison.OrdinalIgnoreCase) || command.Equals("exit", StringComparison.OrdinalIgnoreCase)) {
            network.Disconnect();
            exitRequested = true;
            return;
        }

        if (command.Equals("clear", StringComparison.OrdinalIgnoreCase)) {
            PrintWindow();
            newMessages = false;
            realHistory.Clear();
            formattedHistory.Clear();
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
        
        var sb = new StringBuilder();
        // <sender>
        sb.AppendFormat(
            "{0}<{1}>{2} ", 
            p.SenderName == username ? Terminal.Color.Fg.Cyan : Terminal.Color.Fg.White,
            p.SenderName,
            Terminal.Color.Reset
        );

        // scan for @mentions and highlight them
        var mention = $"@{username}";
        if (p.Message.Contains(mention, StringComparison.OrdinalIgnoreCase)) {
            var idx = p.Message.IndexOf(mention, StringComparison.OrdinalIgnoreCase);
            sb.AppendFormat("{0}{1}{2}{3}{4}",
                p.Message[..idx], 
                Terminal.Color.Fg.Yellow,
                p.Message.Substring(idx, mention.Length),
                Terminal.Color.Reset,
                p.Message[(idx + mention.Length)..]
            );
        } else {
            sb.Append(p.Message);
        }
        NewMessage(sb.ToString());
    }

    void PrintServerMessage(string message) {
        NewMessage($"{Terminal.Color.Fg.Green}{message}{Terminal.Color.Reset}");
    }

    void PrintError(FailPacket packet) {
        NewMessage($"{Terminal.Color.Fg.Red}[!] {packet.Reason}{Terminal.Color.Reset}");
    }
}
