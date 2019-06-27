using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using DiscordHackWeek2019.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Helpers
{
    public struct Purchase
    {
        public BotCommandContext Context { get; set; }
        public ulong MarketId { get; set; }
        public ulong BuyerId { get; set; }
        public string Emoji { get; set; }
    }

    public class Clerk
    {
        public Thread Thread { get; set; }

        private readonly ConcurrentQueue<Purchase> ToProcess = new ConcurrentQueue<Purchase>();
        private readonly ConcurrentQueue<Transaction> ToSave = new ConcurrentQueue<Transaction>();

        public void Process()
        {
            while (true)
            {
                var marketDB = DiscordBot.MainInstance.DataProvider.GetCollection<Market>("markets");

                while (ToProcess.TryDequeue(out var purchase))
                {
                    var market = marketDB.GetById(purchase.MarketId);

                    if (market.Listings.TryGetValue(purchase.Emoji, out var listings) && listings.Count() != 0)
                    {
                        (var lowest, var index) = listings.Select((l, i) => (l, i)).OrderBy(pair => pair.l.Price).FirstOrDefault();

                        listings.RemoveAt(index);

                        marketDB.Update(market);

                        ProcessPurchase(purchase, lowest);
                    }
                }

                while (ToSave.TryDequeue(out var transaction))
                {
                    var market = marketDB.GetById(transaction.MarketId);

                    transaction.Timestamp = DateTimeOffset.Now;
                    market.Transactions.Add(transaction);

                    marketDB.Update(market);
                }
            }
        }

        public void Queue(Purchase purchase)
        {
            ToProcess.Enqueue(purchase);
        }

        public void Queue(Transaction transaction)
        {
            ToSave.Enqueue(transaction);
        }

        private async Task ProcessPurchase(Purchase purchase, Listing listing)
        {
            var ctx = purchase.Context;
            var message = await ctx.Channel.SendMessageAsync($"{ctx.Guild.GetUser(listing.UserId).Mention}, are you sure you want to buy {purchase.Emoji} for {listing.Price}?");

            async void yes(ReactionMessage r)
            {
                var handle = ctx.Channel.SendMessageAsync("Positive response received");

                var user = ctx.UserCollection.GetById(purchase.BuyerId);

                // The inventory wrapper will handle saving for us
                var inventory = ctx.GetInventory(user);

                user.Currency -= listing.Price;

                var emoji = ctx.EmojiCollection.FindById(listing.EmojiId);

                var market = ctx.MarketCollection.GetById(purchase.MarketId);

                var trans = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    MarketId = purchase.MarketId,
                    From = emoji.Owner,
                    To = purchase.BuyerId,
                    Amount = listing.Price,
                };

                Queue(trans);

                emoji.Transactions.Add(trans.GetInfo());
                emoji.Owner = purchase.BuyerId;

                inventory.Add(emoji);

                inventory.Save();
                await handle;
            }

            async void no(ReactionMessage r)
            {

            }

            ReactionMessageHelper.CreateReactionMessage(ctx, message, yes, no);
        }
    }

    public static class MarketHelper
    {
        // Adds a listing to the market, transfering ownership of the emoji from the seller to the market
        // Returns the added listing
        public static Listing AddListing(BotCommandContext context, ulong marketId, Emoji emoji, int price)
        {
            // Get the corresponding market
            var marketsDB = context.MarketCollection;
            var market = marketsDB.GetById(marketId);
            if (market == null)
            {
                // Market does not exist, so make a new one
                market = new Market
                {
                    MarketId = marketId,
                    Transactions = new List<Transaction>(),
                    Listings = new Dictionary<string, List<Listing>>()
                };
                marketsDB.Insert(market);
            }

            // Add listing to the market
            if (!market.Listings.ContainsKey(emoji.Unicode))
            {
                // Make a new list of listings in the listing list if there is not a listing list to listen for listings
                market.Listings[emoji.Unicode] = new List<Listing>();
            }
            Listing toAdd = new Listing
            {
                UserId = context.User.Id,
                EmojiId = emoji.EmojiId,
                Price = price,
                Timestamp = DateTimeOffset.Now
            };
            market.Listings[emoji.Unicode].Add(toAdd);
            marketsDB.Update(market);

            return toAdd;
        }

        // Lets a user buy a listing, transfering ownership of the emoji from the market to the buyer
        public static void BuyListing(BotCommandContext context, ulong marketId, string emojiUnicode, ulong buyer)
        {
            // TODO: Record transaction in market and emoji
            // TODO: Add to inventory
        }

        // Gets the emoji price. Returns float.NaN if there are none for sale
        public static (int, bool valid) GetEmojiPrice(BotCommandContext context, ulong marketId, string emojiUnicode)
        {
            // Make sure market exists
            var marketsDB = context.MarketCollection;
            var market = marketsDB.GetById(marketId);
            if (market == null)
            {
                // Market does not exist
                return (0, false);
            }

            if (market.Listings.ContainsKey(emojiUnicode))
            {
                Listing cheapest = CheapestListing(market.Listings[emojiUnicode]);
                if (cheapest != null)
                {
                    // There is a valid listing for sale
                    return (cheapest.Price, true);
                }
            }

            return (0, false);
        }

        private static Listing CheapestListing(List<Listing> listings)
        {
            return listings.OrderBy(listing => listing.Price).FirstOrDefault();
        }
    }
}
