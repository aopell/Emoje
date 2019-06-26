using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Listing
    {
        public ulong UserId { get; set; }
        public ulong EmojiId { get; set; }
        public float Price { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
