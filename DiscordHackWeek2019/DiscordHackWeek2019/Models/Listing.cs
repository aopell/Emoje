﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Listing
    {
        public ulong SellerId { get; set; }
        public Guid EmojiId { get; set; }
        public long Price { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
