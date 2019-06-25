using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Transaction
    {
        public ulong TransactionId { get; set; }
        public ulong From { get; set; }
        public ulong To { get; set; }
        public float Amount { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
