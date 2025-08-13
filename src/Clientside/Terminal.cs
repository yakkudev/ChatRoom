using System.Text;

namespace ChatRoom.Clientside;

// ❤️
// https://gist.github.com/fnky/458719343aabd01cfb17a3a4f7296797
// 

public static class Terminal {
    public static int Width => Console.WindowWidth;
    public static int Height => Console.WindowHeight;
    
    static readonly StringBuilder Output = new();
    
    public static void Write(string text) {
        Output.Append(text);
    }
    
    public static void Write(params string[] texts) {
        foreach (var text in texts) {
            Output.Append(text);
        }
    }
    
    public static void Flush() {
        Console.Write(Output.ToString());
        Output.Clear();
    }
    
    public static void Clear() => Write("\x1b[2J\x1b[H");
    public static void ClearLine() => Write("\x1b[2K");
    
    public static void CursorStart() => Write("\x1b[H");
    public static void CursorTo(int x, int y) => Write($"\x1b[{y};{x}H");
    public static void CursorUp(int n = 1) => Write($"\x1b[{n}A");
    public static void CursorDown(int n = 1) => Write($"\x1b[{n}B");
    public static void CursorForward(int n = 1) => Write($"\x1b[{n}C");
    public static void CursorBackward(int n = 1) => Write($"\x1b[{n}D");
    public static void CursorNewLine() => Write("\x1b[1E");
    public static void CursorLastLine() => Write("\x1b[1F");

    public static class Color {
        public static string Reset => "\x1b[0m";
        public static string Inverse => "\x1b[7m";

        public static class Fg {
            public static string Black => "\x1b[30m";
            public static string Red => "\x1b[31m";
            public static string Green => "\x1b[32m";
            public static string Yellow => "\x1b[33m";
            public static string Blue => "\x1b[34m";
            public static string Magenta => "\x1b[35m";
            public static string Cyan => "\x1b[36m";
            public static string White => "\x1b[37m";
            public static string Default => "\x1b[39m";
        }
        
        public static class Bg {
            public static string Black => "\x1b[40m";
            public static string Red => "\x1b[41m";
            public static string Green => "\x1b[42m";
            public static string Yellow => "\x1b[43m";
            public static string Blue => "\x1b[44m";
            public static string Magenta => "\x1b[45m";
            public static string Cyan => "\x1b[46m";
            public static string White => "\x1b[47m";
            public static string Default => "\x1b[49m";
        }
    }
}