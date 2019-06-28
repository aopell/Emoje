using Discord;
using LiteDB;
using OneOf;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Transaction
    {
        public ulong TransactionId { get; set; }
        public ulong MarketId { get; set; }
        public TransactionData Data { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        [BsonIgnore]
        public ulong Acquisitor => Data.Acquisitor;

        [BsonIgnore]
        public Optional<ulong> Seller => (Data as BetweenUsers)?.Seller ?? Optional.Create<ulong>();

        public TransactionInfo GetInfo() => new TransactionInfo { MarketId = MarketId, TransactionId = TransactionId };

        public static Transaction FromLootbox(ulong marketId, ulong buyer, string lootbox)
        {
            return new Transaction
            {
                MarketId = marketId,
                Data = new FromLootbox
                {
                    Acquisitor = buyer,
                    LootboxType = lootbox,
                },
            };
        }

        public static Transaction BetweenUsers(Listing listing, ulong marketId, ulong buyerId)
        {
            return new Transaction
            {
                MarketId = marketId,
                Data = new BetweenUsers
                {
                    Acquisitor = buyerId,
                    Seller = listing.SellerId,
                    Price = listing.Price,
                },
            };
        }
    }

    public class TransactionData
    {
        public ulong Acquisitor { get; set; }
    }

    public class BetweenUsers : TransactionData
    {
        public ulong Seller { get; set; }
        public int Price { get; set; }
    }

    public class FromLootbox : TransactionData
    {
        public string LootboxType { get; set; }
    }
}
