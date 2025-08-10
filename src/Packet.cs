using System.Reflection;

namespace ChatRoom;

public enum PacketType : byte {
    Fail,
    Ok,
    SendConnect,
    AssignSession,
    Rename,
    SendChatMessage,
    ChatMessage,
    Display, // display stuff requested by server
    SendCommand // send cmd to server
}

public abstract class Packet {
    protected abstract PacketType Type { get; }

    public void WriteTo(BinaryWriter writer) {
        // todo: don't write everything as a string
        // todo: add packet length
        writer.Write((byte)Type);
        foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (prop.CanRead) {
                writer.Write(prop.GetValue(this)?.ToString() ?? "");
            }
        }
    }

    public static T ReadFrom<T>(BinaryReader reader) where T : Packet, new() {
        // this assumes packet header is already read
        var packet = new T();
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (prop.CanWrite) {
                var val = reader.ReadString();
                prop.SetValue(packet, val);
            }
        }

        return packet;
    }
}