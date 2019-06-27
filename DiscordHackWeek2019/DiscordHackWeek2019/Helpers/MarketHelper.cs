﻿using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using DiscordHackWeek2019.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                UserId = context.User.Id,
                EmojiId = emoji.EmojiId,
                Price = price,
                Timestamp = DateTimeOffset.Now
            };

            context.Bot.Clerk.Queue(PostListing.InMarket(emoji.Unicode, marketId, toAdd));

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
        // Returns null if the market does not exist
        public static Dictionary<Rarity, List<string>> GetRarities(BotCommandContext context, ulong marketId)
        {
            // Make sure market exists
            var marketsDB = context.Bot.DataProvider.GetCollection<Market>("markets");
            var market = marketsDB.GetById(marketId);
            if (market == null)
            {
                // Market does not exist
                return null;
            }

            // Get allll the prices
            List<KeyValuePair<int, string>> prices = new List<KeyValuePair<int, string>>();
            foreach (string key in market.Listings.Keys)
            {
                var listing = CheapestListing(market.Listings[key]);
                prices.Add(new KeyValuePair<int, string>(listing.Price, key));
            }

            // Set up the results dictionary
            Dictionary<Rarity, List<string>> result = new Dictionary<Rarity, List<string>>();
            foreach (Rarity rarity in Rarity.Rarities)
            {
                result[rarity] = new List<string>();
            }

            // Seperate into rarities
            var pricesEnum = prices.OrderBy(emojiPair => emojiPair.Key);
            int pos = 0;
            foreach (var emojiPair in pricesEnum)
            {
                int rank = ((pos + 1) * 100) / pricesEnum.Count();
                foreach (Rarity rarity in Rarity.Rarities)
                {
                    if (rank <= rarity.PercentMax)
                    {
                        result[rarity].Add(emojiPair.Value);
                        break;
                    }
                }
                pos++;
            }

            return result;
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

    public class Clerk
    {
        public Thread Thread { get; set; }

        private readonly ConcurrentQueue<PostListing> ToPost = new ConcurrentQueue<PostListing>();
        private readonly ConcurrentQueue<Purchase> ToProcess = new ConcurrentQueue<Purchase>();
        private readonly ConcurrentQueue<Transaction> ToSave = new ConcurrentQueue<Transaction>();

        public void Process()
        {
            while (true)
            {
                var marketDB = DiscordBot.MainInstance.DataProvider.GetCollection<Market>("markets");

                while (ToPost.TryDequeue(out var newListing))
                {
                    var market = GetOrCreate(marketDB, newListing.MarketId);

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
                }

                while (ToProcess.TryDequeue(out var purchase))
                {
                    var market = GetOrCreate(marketDB, purchase.MarketId);

                    if (market.Listings.TryGetValue(purchase.Emoji, out var listings) && listings.Count() != 0)
                    {
                        (var lowest, var index) = listings.Select((l, i) => (l, i)).OrderBy(pair => pair.l.Price).FirstOrDefault();

                        listings.RemoveAt(index);

                        marketDB.Upsert(market);

                        ProcessPurchase(purchase, lowest);
                    }
                }

                while (ToSave.TryDequeue(out var transaction))
                {
                    var market = GetOrCreate(marketDB, transaction.MarketId);

                    transaction.Timestamp = DateTimeOffset.Now;
                    market.Transactions.Add(transaction);

                    marketDB.Upsert(market);
                }
            }
        }

        public void Queue(PostListing listing)
        {
            ToPost.Enqueue(listing);
        }

        public void Queue(Purchase purchase)
        {
            ToProcess.Enqueue(purchase);
        }

        public void Queue(Transaction transaction)
        {
            ToSave.Enqueue(transaction);
        }

        private Market GetOrCreate(LiteDB.LiteCollection<Market> marketDB, ulong marketId)
        {
            var market = marketDB.GetById(marketId);

            if (market == null)
            {
                // Market does not exist, so make a new one
                return new Market
                {
                    MarketId = marketId,
                    Transactions = new List<Transaction>(),
                    Listings = new Dictionary<string, List<Listing>>()
                };
            }

            return market;
        }

        private async Task ProcessPurchase(Purchase purchase, Listing listing)
        {
            var ctx = purchase.Context;
            var buyer = ctx.Guild.GetUser(purchase.BuyerId);
            var message = await ctx.Channel.SendMessageAsync($"{buyer.Mention}, are you sure you want to buy {purchase.Emoji} for {listing.Price}?");

            async Task yes(ReactionMessage r)
            {
                var replyHandle = message.ModifyAsync(properties => properties.Content = $"{ctx.WhatDoICall(buyer.Id)} bought {purchase.Emoji} for {listing.Price}");

                var buyerProfile = ctx.UserCollection.GetById(purchase.BuyerId);

                if (listing.UserId == buyer.Id)
                {
                    buyer.GetOrCreateDMChannelAsync().ContinueWith(async task =>
                    {
                        if (task.IsFaulted) return; // TODO: log error

                        await task.Result.SendMessageAsync($"You bought your own {purchase.Emoji} for {listing.Price}. You still have {buyerProfile.Currency}");
                    });
                }
                else
                {
                    var seller = ctx.Bot.Client.GetUser(listing.UserId);
                    var sellerProfile = ctx.GetProfile(seller);

                    buyerProfile.Currency -= listing.Price;
                    sellerProfile.Currency += listing.Price;

                    seller.GetOrCreateDMChannelAsync().ContinueWith(async task =>
                    {
                        if (task.IsFaulted) return; // TODO: log error

                        await task.Result.SendMessageAsync($"{buyer.ToString()} bought your {purchase.Emoji} for {listing.Price}. You now have {sellerProfile.Currency}");
                    });
                    Task.Run(() => ctx.UserCollection.Update(sellerProfile));
                }

                var emoji = ctx.EmojiCollection.FindById(listing.EmojiId);

                var market = ctx.MarketCollection.GetById(purchase.MarketId);

                var trans = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    MarketId = purchase.MarketId,
                    From = listing.UserId,
                    To = purchase.BuyerId,
                    Amount = listing.Price,
                };

                Queue(trans);

                emoji.Transactions.Add(trans.GetInfo());
                emoji.Owner = purchase.BuyerId;

                // The inventory wrapper will handle saving for us
                var inventory = ctx.GetInventory(buyerProfile);
                inventory.Add(emoji);

                inventory.Save();
            }

            async void cancelled()
            {
                var handle = message.ModifyAsync(properties => properties.Content = $"Purchase of {purchase.Emoji} cancelled");

                Queue(PostListing.InMarket(purchase.Emoji, purchase.MarketId, listing));

                await handle;
            }

            async Task no(ReactionMessage r)
            {
                var handle = r.Message.ModifyAsync(properties => properties.Content = $"Purchase of {purchase.Emoji} cancelled");

                Queue(PostListing.InMarket(purchase.Emoji, purchase.MarketId, listing));

                await handle;
            }


            ReactionMessageHelper.CreateReactionMessage(ctx, message, yes, no, onTimeout: cancelled);
        }
    }
}

public sealed class Rarity
{
    public static Rarity Common = new Rarity(0, 40, "Common", System.Drawing.Color.White);
    public static Rarity Rare = new Rarity(40, 70, "Rare", System.Drawing.Color.SkyBlue);
    public static Rarity Epic = new Rarity(70, 90, "Epic", System.Drawing.Color.Purple);
    public static Rarity Legendary = new Rarity(90, 100, "Legendary", System.Drawing.Color.Orange);

    // Must be in order (Least rare first)
    public static Rarity[] Rarities = { Common, Rare, Epic, Legendary };

    public int PercentMax { get; }
    public int PercentMin { get; }
    public string Label { get; }
    public System.Drawing.Color Color { get; }

    public int Percent()
    {
        return PercentMax - PercentMin;
    }

    private Rarity(int percentMin, int percentMax, String label, System.Drawing.Color color)
    {
        PercentMin = percentMin;
        PercentMax = percentMax;
        Label = label;
        Color = color;
    }
}
