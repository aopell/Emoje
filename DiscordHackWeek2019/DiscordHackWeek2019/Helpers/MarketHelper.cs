using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using DiscordHackWeek2019.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using System.Threading.Tasks.Dataflow;

namespace DiscordHackWeek2019.Helpers
{
    public static class MarketHelper
    {
        // Adds a listing to the market, transfering ownership of the emoji from the seller to the market
        // Returns the added listing
        public static Listing AddListing(BotCommandContext context, ulong marketId, Emoji emoji, int price)
        {
            if (emoji.EmojiId == Guid.Empty) throw new ArgumentException("Error: cannot sell emoji without id");

            Listing toAdd = new Listing
            {
                SellerId = context.User.Id,
                EmojiId = emoji.EmojiId,
                Price = price,
            };

            context.Bot.Clerk.Queue(PostListing.InMarket(emoji.Unicode, marketId, toAdd, true));

            return toAdd;
        }

        // Lets a user buy a listing, transfering ownership of the emoji from the market to the buyer
        public static void BuyListing(BotCommandContext context, ulong marketId, string emojiUnicode, ulong buyer)
        {
            context.Bot.Clerk.Queue(new Purchase
            {
                Context = context,
                MarketId = marketId,
                BuyerId = buyer,
                Emoji = emojiUnicode,
            }
            );
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

        // Get a dictionary of rarities from rarity to a list of emojis for the given market
        public static Dictionary<Rarity, List<string>> GetRarities(DiscordBot bot, ulong marketId)
        {
            // Make sure market exists
            var marketsDB = bot.DataProvider.GetCollection<Market>("markets");
            var market = GetOrCreate(marketsDB, marketId, true);

            List <KeyValuePair<int, string>> prices = new List<KeyValuePair<int, string>>();
            foreach (string key in EmojiHelper.IterateAllEmoji)
            {
                if (market.Listings.ContainsKey(key) && market.Listings[key].Count > 0)
                {
                    var listing = CheapestListing(market.Listings[key]);
                    prices.Add(new KeyValuePair<int, string>(listing.Price, key));
                }
                else
                {
                    prices.Add(new KeyValuePair<int, string>(0, key));
                }
            }

            // Set up the results dictionary
            Dictionary<Rarity, List<string>> result = new Dictionary<Rarity, List<string>>();
            foreach (Rarity rarity in Rarity.Rarities)
            {
                result[rarity] = new List<string>();
            }

            // Seperate into rarities
            var pricesEnum = prices.OrderBy(emojiPair => emojiPair.Key).ToList();
            int pos = 0;
            foreach (var emojiPair in pricesEnum)
            {
                int rank = ((pos + 1) * 100) / pricesEnum.Count;
                int percentSum = 0;
                foreach (Rarity rarity in Rarity.Rarities)
                {
                    percentSum += rarity.Percent;
                    if (rank <= percentSum)
                    {
                        result[rarity].Add(emojiPair.Value);
                        break;
                    }
                }
                pos++;
            }

            return result;
        }

        public static Market GetOrCreate(LiteDB.LiteCollection<Market> marketDB, ulong marketId, bool insertIfNew = false)
        {
            var market = marketDB.GetById(marketId);

            if (market == null)
            {
                market = new Market
                {
                    MarketId = marketId,
                    Transactions = new List<Transaction>(),
                    Listings = new Dictionary<string, List<Listing>>()
                };

                if (insertIfNew) marketDB.Insert(market);
            }

            return market;
        }

        private static Listing CheapestListing(List<Listing> listings)
        {
            return listings.OrderBy(listing => listing.Price).FirstOrDefault();
        }
    }

    public struct Purchase
    {
        public BotCommandContext Context { get; set; }
        public ulong MarketId { get; set; }
        public ulong BuyerId { get; set; }
        public string Emoji { get; set; }
    }

    public struct PostListing
    {
        public Listing Listing { get; set; }
        public ulong MarketId { get; set; }
        public string Emoji { get; set; }
        public bool IsNew { get; set; }

        public static PostListing InGlobal(string emoji, Listing listing, bool isNew = false)
        {
            return new PostListing
            {
                Listing = listing,
                MarketId = 0,
                Emoji = emoji,
                IsNew = isNew
            };
        }

        public static PostListing InMarket(string emoji, ulong marketId, Listing listing, bool isNew = false)
        {
            return new PostListing
            {
                Listing = listing,
                MarketId = marketId,
                Emoji = emoji,
                IsNew = isNew
            };
        }
    }

    public struct NewTransaction
    {
        public Transaction Transaction { get; set; }
        public BufferBlock<TransactionInfo> IdCallback { get; set; }
    }

    public class Clerk
    {
        private readonly Thread Thread;

        private readonly BlockingCollection<OneOf<PostListing, Purchase, NewTransaction>> ToProcess = new BlockingCollection<OneOf<PostListing, Purchase, NewTransaction>>(new ConcurrentQueue<OneOf<PostListing, Purchase, NewTransaction>>());

        public Clerk()
        {
            Thread = new Thread(Process);
            Thread.Start();
        }

        /// <summary>
        /// Queues the processing of a Transaction and returns a way to get the transaction Id
        /// </summary>
        /// <param name="listing"></param>
        /// <returns></returns>
        public BufferBlock<TransactionInfo> Queue(Transaction listing)
        {
            var buffer = new BufferBlock<TransactionInfo>();
            Queue(new NewTransaction
            {
                Transaction = listing,
                IdCallback = buffer,
            });

            return buffer;
        }

        public void Queue(OneOf<PostListing, Purchase, NewTransaction> listing)
        {
            ToProcess.Add(listing);
        }

        private void Process()
        {
            var marketDB = DiscordBot.MainInstance.DataProvider.GetCollection<Market>("markets");

            foreach (var thing in ToProcess.GetConsumingEnumerable())
            {
                thing.Switch(
                    newListing =>
                    {
                        var market = MarketHelper.GetOrCreate(marketDB, newListing.MarketId);

                        if (newListing.IsNew) newListing.Listing.Timestamp = DateTimeOffset.Now;

                        List<Listing> list;
                        if (!market.Listings.ContainsKey(newListing.Emoji))
                        {
                            list = new List<Listing>();
                            market.Listings.Add(newListing.Emoji, list);
                        }
                        else
                        {
                            list = market.Listings[newListing.Emoji];
                        }

                        list.Add(newListing.Listing);

                        marketDB.Upsert(market);
                    },
                    purchase =>
                    {
                        var market = MarketHelper.GetOrCreate(marketDB, purchase.MarketId);

                        if (market.Listings.TryGetValue(purchase.Emoji, out var listings) && listings.Count() != 0)
                        {
                            (var lowest, var index) = listings.Select((l, i) => (l, i)).OrderBy(pair => pair.l.Price).FirstOrDefault();

                            listings.RemoveAt(index);

                            marketDB.Upsert(market);

                            ProcessPurchase(purchase, lowest);
                        }
                    },
                    trans =>
                    {
                        var market = MarketHelper.GetOrCreate(marketDB, trans.Transaction.MarketId);

                        trans.Transaction.Timestamp = DateTimeOffset.Now;

                        ulong newId = (ulong) market.Transactions.LongCount();
                        trans.Transaction.TransactionId = newId;

                        market.Transactions.Add(trans.Transaction);

                        marketDB.Upsert(market);

                        trans.IdCallback.Post(trans.Transaction.GetInfo());
                    }
                );
            }
        }

        private async Task ProcessPurchase(Purchase purchase, Listing listing)
        {
            var ctx = purchase.Context;
            var buyer = ctx.Guild.GetUser(purchase.BuyerId);
            var message = await ctx.Channel.SendMessageAsync($"{buyer.Mention}, are you sure you want to buy {purchase.Emoji} for {ctx.Money(listing.Price)}?");

            ReactionMessageHelper.CreateConfirmReactionMessage(ctx, message, 
                async onOkay =>
                {
                    var replyHandle = message.ModifyAsync(properties => properties.Content = $"{ctx.WhatDoICall(buyer.Id)} bought {purchase.Emoji} for {ctx.Money(listing.Price)}");

                    var buyerProfile = ctx.UserCollection.GetById(purchase.BuyerId);

                    if (listing.SellerId == buyer.Id)
                    {
                        buyer.GetOrCreateDMChannelAsync().ContinueWith(async task =>
                        {
                            if (task.IsFaulted) return; // TODO: log error

                            await task.Result.SendMessageAsync($"You bought your own {purchase.Emoji} for {listing.Price}. You still have {ctx.Money(buyerProfile.Currency)}");
                        });
                    }
                    else
                    {
                        var seller = ctx.Bot.Client.GetUser(listing.SellerId);
                        var sellerProfile = ctx.GetProfile(seller);

                        buyerProfile.Currency -= listing.Price;
                        sellerProfile.Currency += listing.Price;

                        seller.GetOrCreateDMChannelAsync().ContinueWith(async task =>
                        {
                            if (task.IsFaulted) return; // TODO: log error

                            await task.Result.SendMessageAsync($"{buyer.ToString()} bought your {purchase.Emoji} for {listing.Price}. You now have {ctx.Money(sellerProfile.Currency)}");
                        });
                        Task.Run(() => ctx.UserCollection.Update(sellerProfile));
                    }

                    var emoji = ctx.EmojiCollection.FindById(listing.EmojiId);

                    var market = ctx.MarketCollection.GetById(purchase.MarketId);

                    var trans = Transaction.BetweenUsers(listing, purchase.MarketId, purchase.BuyerId);

                    emoji.Transactions.Add(Queue(trans).Receive());
                    emoji.Owner = purchase.BuyerId;

                    // The inventory wrapper will handle saving for us
                    var inventory = ctx.GetInventory(buyerProfile);
                    inventory.Add(emoji);

                    inventory.Save();
                }, 
                async onDecline =>
                {
                    var handle = message.ModifyAsync(properties => properties.Content = $"Purchase of {purchase.Emoji} cancelled");

                    Queue(PostListing.InMarket(purchase.Emoji, purchase.MarketId, listing));

                    await handle;
                }, 
                onTimeout: async () =>
                {
                var handle = message.ModifyAsync(properties => properties.Content = $"Purchase of {purchase.Emoji} cancelled");

                Queue(PostListing.InMarket(purchase.Emoji, purchase.MarketId, listing));

                await handle;
            });
        }
    }
}

public sealed class Rarity
{
    public static Rarity Common = new Rarity(40, "Common", System.Drawing.Color.White, Strings.commonLeft, Strings.commonRight);
    public static Rarity Rare = new Rarity(30, "Rare", System.Drawing.Color.SkyBlue, Strings.rareLeft, Strings.rareRight);
    public static Rarity Epic = new Rarity(20, "Epic", System.Drawing.Color.Purple, Strings.epicLeft, Strings.epicRight);
    public static Rarity Legendary = new Rarity(10, "Legendary", System.Drawing.Color.Orange, Strings.legendaryLeft, Strings.legendaryRight);

    // Must be in order (Least rare first)
    public static Rarity[] Rarities = { Common, Rare, Epic, Legendary };

    public int Percent { get; }
    public string Label { get; }
    public System.Drawing.Color Color { get; }
    public string LeftBracket { get; }
    public string RightBracket { get; }

    private Rarity(int percent, String label, System.Drawing.Color color, string leftBracket, string rightBracket)
    {
        Percent = percent;
        Label = label;
        Color = color;
        LeftBracket = leftBracket;
        RightBracket = rightBracket;
    }
}
