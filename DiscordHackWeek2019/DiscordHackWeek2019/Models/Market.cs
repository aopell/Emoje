using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Market
    {
        public ulong MarketId { get; set; }
        public List<Transaction> Transactions { get; set; }
        public Dictionary<string, List<Listing>> Listings { get; set; }
    }
}
