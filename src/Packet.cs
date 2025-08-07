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
    SendCommand, // send cmd to server
}

public abstract class Packet {
    public abstract PacketType Type { get; }

    public virtual void WriteTo(BinaryWriter binaryWriter) {
        binaryWriter.Write((byte)Type);
    }
}

