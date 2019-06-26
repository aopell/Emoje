using System.Collections.Generic;
using System.IO;

namespace DiscordHackWeek2019.Helpers
{
    class EmojiHelper
    {
        public IReadOnlyCollection<string> IterateAllEmoji { get => EmojiToName.Keys; }

        private readonly Dictionary<string, string> EmojiToName = new Dictionary<string, string>();
        private readonly Dictionary<string, string> NameToEmoji = new Dictionary<string, string>();

        public EmojiHelper(string emojiTableName)
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

        public string GetNameFromEmoji(string emoji) => EmojiToName[emoji];

        public string GetEmojiFromName(string name) => NameToEmoji[name];
    }
}
