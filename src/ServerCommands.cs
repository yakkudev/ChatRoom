using ChatRoom.Packets;

namespace ChatRoom;

public partial class Server {
    
    Dictionary<string, Command> commands = new();
    void InitCommands() {
        commands = new Dictionary<string, Command> {
            { "list", new Command("list", "List connected users", "/list", false, ExecuteListCommand) },
            { "help", new Command("help", "Show available commands", "/help", false, ExecuteHelpCommand) },
            { "kick", new Command("kick", "Kick a user from the server", "/kick <username>", true, ExecuteKickCommand) },
            { "op", new Command("op", "Grant privileges to a user", "/op <username>", true,ExecuteOpCommand) },
            { "deop", new Command("deop", "Revoke privileges from a user", "/deop <username>", true, ExecuteDeopCommand) }
        };
    }
    
    void ExecuteKickCommand(Client client, string[] args) {
        var targetName = args[1];
        var targetClient = GetClientByName(targetName);
        if (targetClient == null) {
            client.SendPacket(new FailPacket($"user <{targetName}> not found"));
            Console.WriteLine($"<{client.Name}> tried to kick non-existing user <{targetName}>");
            return;
        }
        
        targetClient.SendPacket(new FailPacket("Kicked from the server."));
        DropClient(targetClient);
        BroadcastPacket(new ChatMessagePacket("$", $"User <{targetClient.Name}> was kicked by <{client.Name}>."), client);
        Console.WriteLine($"<{client.Name}> kicked <{targetClient.Name}>");
    }

    void ExecuteHelpCommand(Client client, string[] args) {
        var helpMessage = "Available commands:\n";
        foreach (var cmd in commands.Values) {
            if (!client.Privileged && cmd.RequiresPrivilege)
                continue;
            helpMessage += $"\t{cmd.Usage} - {cmd.Description}\n";
        }
        client.SendPacket(new ChatMessagePacket("$", helpMessage));
    }

    void ExecuteListCommand(Client client, string[] args) {
        List<string> names;
        lock (threadLock) {
            names = clients.Select(c => c.Name).ToList();
        }
        client.SendPacket(new ChatMessagePacket("$", $"Connected users: {string.Join(", ", names)}"));
        Console.WriteLine($"<{client.Name}> requested user list.");
    }
    
    void ExecuteOpCommand(Client client, string[] args) {
        var targetName = args[1];
        var targetClient = GetClientByName(targetName);
        if (targetClient == null) {
            client.SendPacket(new FailPacket($"user <{targetName}> not found"));
            Console.WriteLine($"<{client.Name}> tried to op non-existing user <{targetName}>");
            return;
        }

        if (targetClient.Privileged) {
            client.SendPacket(new FailPacket($"user <{targetName}> is already privileged"));
            return;
        }

        targetClient.Privileged = true;
        targetClient.SendPacket(new ChatMessagePacket("$", $"You were granted admin privileges by <{client.Name}>."));
        client.SendPacket(new ChatMessagePacket("$", $"User <{targetName}> was granted admin privileges."));
        Console.WriteLine($"<{client.Name}> granted privileges to <{targetClient.Name}>");
    }
    
    void ExecuteDeopCommand(Client client, string[] args) {
        var targetName = args[1];
        var targetClient = GetClientByName(targetName);
        if (targetClient == null) {
            client.SendPacket(new FailPacket($"user <{targetName}> not found"));
            Console.WriteLine($"<{client.Name}> tried to deop non-existing user <{targetName}>");
            return;
        }

        if (!targetClient.Privileged) {
            client.SendPacket(new FailPacket($"user <{targetName}> is not privileged"));
            return;
        }

        targetClient.Privileged = false;
        targetClient.SendPacket(new ChatMessagePacket("$", $"Your admin privileges were revoked by <{client.Name}>."));
        client.SendPacket(new ChatMessagePacket("$", $"User <{targetName}> had their admin privileges revoked."));
        Console.WriteLine($"<{client.Name}> revoked privileges from <{targetClient.Name}>");
    }
}