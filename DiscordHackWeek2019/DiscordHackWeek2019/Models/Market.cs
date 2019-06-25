using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Market
    {
        public ulong MarketId { get; set; }
        public Dictionary<string, IEnumerable<Listing>> Listings { get; set; }
    }
}
