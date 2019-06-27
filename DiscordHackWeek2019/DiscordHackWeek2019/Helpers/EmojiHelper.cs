using System.Collections.Generic;
using System.IO;

namespace DiscordHackWeek2019.Helpers
{
    public static class EmojiHelper
    {
        public static IReadOnlyCollection<string> IterateAllEmoji { get => EmojiToName.Keys; }

        private static readonly Dictionary<string, string> EmojiToName = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> NameToEmoji = new Dictionary<string, string>();

        public static void Initialize(string emojiTableName)
        {
            using (var emojiTable = new StreamReader(File.OpenRead(emojiTableName)))
            {
                while (!emojiTable.EndOfStream)
                {
                    (var emoji, var name, var rest) = emojiTable.ReadLine().Split('\t');

                    EmojiToName[emoji] = name;
                    NameToEmoji[name] = emoji;
                }
            }
        }

        public static string GetNameFromEmoji(string emoji) => EmojiToName[emoji];

        public static string GetEmojiFromName(string name) => NameToEmoji[name];

        public static bool IsValidEmoji(string emoji) => EmojiToName.ContainsKey(emoji);
    }
}
