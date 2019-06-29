using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class User
    {
        public ulong UserId { get; set; }
        public bool Disabled { get; set; }
        public long Currency { get; set; }
        public Dictionary<string, int> LootBoxes { get; set; }
        public PortfolioCollection Investments { get; set; }
        public Dictionary<string, List<Guid>> Inventory { get; set; }
        public List<TransactionInfo> Transactions { get; set; }
    }
}
