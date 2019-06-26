using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Models
{
    public class Emoji
    {
        public Guid EmojiId { get; set; }
        public string Unicode { get; set; }
        public ulong Owner { get; set; }
        public List<TransactionInfo> Transactions { get; set; }
    }
}
