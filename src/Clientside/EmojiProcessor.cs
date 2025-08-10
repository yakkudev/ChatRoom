using System.Text.RegularExpressions;

namespace ChatRoom.Clientside;

public class EmojiProcessor {
    static readonly Dictionary<string, string> EmojiMap = new() {
        { "fish", "\U0001F41F" },
        { "sob", "\U0001F62D" },
        { "skull", "\U0001F480" },
        { "heart", "\u2764\uFE0F" },
        { "fire", "\U0001F525" },
        { "smirk_cat", "\U0001F63C" },
        { "pregnant_man", "\U0001FAC3" },
        { "nerd", "\U0001F913" },
        { "broken_heart", "\U0001F494" },
        { "wilted_rose", "\U0001F940" },
        { "pray", "\U0001F64F" } 
    };

    static readonly Regex EmojiRegex = new(":(.*?):");

    public static string Emojify(string inputText) {
        if (string.IsNullOrEmpty(inputText)) {
            return string.Empty;
        }

        return EmojiRegex.Replace(inputText, match => {
            var shortcode = match.Groups[1].Value;

            if (EmojiMap.TryGetValue(shortcode, out var emoji)) {
                return emoji;
            }
            return match.Value;
        });
    }
}