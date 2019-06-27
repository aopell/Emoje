using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public struct TransactionInfo
    {
        public ulong MarketId { get; set; }
        public Guid TransactionId { get; set; }
    }
}
