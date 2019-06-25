using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Emoji
    {
        public ulong EmojiId { get; set; }
        public string Unicode { get; set; }
        public User Owner { get; set; }
        public IEnumerable<TransactionInfo> Transactions { get; set; }
    }
}
