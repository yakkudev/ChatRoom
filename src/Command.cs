namespace ChatRoom;

public class Command {
    public Command(string name, string description, string usage, bool requiresPrivilege,
        Action<Client, string[]> action) {
        Name = name;
        Description = description;
        Usage = usage;
        Action = action;
        RequiresPrivilege = requiresPrivilege;
    }

    public string Name { get; }
    public string Description { get; }
    public string Usage { get; }
    public Action<Client, string[]> Action { get; }
    public bool RequiresPrivilege { get; set; }
    public int MinArgs => Usage.Split(' ').Length - 1; // usage format: "command arg1 arg2 ..."

    public void Execute(Client client, string[] args) {
        Action(client, args);
    }
}