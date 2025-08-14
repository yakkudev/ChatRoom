using System.Text;

namespace ChatRoom.Clientside;

public class TextField(int maxLength = 50) {
    public StringBuilder Text { get; set; } = new();
    public int CursorPosition { get; set; } = 0;

    public int MaxLength {
        get => maxLength;
        set {
            if (value < 0) {
                throw new ArgumentOutOfRangeException(nameof(value), "MaxLength cannot be negative.");
            }
            if (Text.Length > value) {
                Text.Length = value;
            }
            maxLength = value;
            MoveCursor(0);
        }
    }

    public void Insert(char c) {
        if (Text.Length >= MaxLength) return;
        Text.Insert(CursorPosition, c);
        CursorPosition++;
    }

    public void DeleteBack() {
        if (CursorPosition <= 0) return;
        Text.Remove(CursorPosition - 1, 1);
        CursorPosition--;
    }
    
    public void DeleteForward() {
        if (CursorPosition >= Text.Length) return;
        Text.Remove(CursorPosition, 1);
    }
    
    public void MoveCursor(int offset) {
        CursorPosition += offset;
        CursorPosition = Math.Clamp(CursorPosition, 0, Text.Length);
    }
    
    public void Clear() {
        Text.Clear();
        CursorPosition = 0;
    }
    
    public override string ToString() {
        return Text.ToString();
    }
}