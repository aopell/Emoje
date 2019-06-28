using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiscordHackWeek2019.Helpers
{
    public static class EmojiHelper
    {
        public static IReadOnlyCollection<string> IterateAllEmoji { get => EmojiToName.Keys; }

        private static Dictionary<string, string> EmojiToName = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> NameToEmoji = new Dictionary<string, string>();

        public static void Initialize(string emojiTableName, Random random)
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

            EmojiToName = EmojiToName.ToList().OrderBy(kv => random.Next()).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public static string GetNameFromEmoji(string emoji) => EmojiToName[emoji];

        public static string GetEmojiFromName(string name) => NameToEmoji[name];

        public static bool IsValidEmoji(string emoji) => EmojiToName.ContainsKey(emoji);
    }
}
