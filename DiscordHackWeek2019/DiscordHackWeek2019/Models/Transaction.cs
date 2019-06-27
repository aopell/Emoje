using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public ulong MarketId { get; set; }
        public ulong From { get; set; }
        public ulong To { get; set; }
        public int Amount { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public TransactionInfo GetInfo() => new TransactionInfo { MarketId = MarketId, TransactionId = TransactionId };
    }
}
