using Discord.Commands;
using DiscordHackWeek2019.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DiscordHackWeek2019.Models;
using Discord;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("market"), Alias("m"), JoinRequired]
    public class MarketModule : ModuleBase<BotCommandContext>
    {
        public enum Market
        {
            Global = 0,
            Local = 1,
            G = 0,
            L = 1,
            S = 1,
            Guild = 1,
            Server = 1,
        }

        private ulong MarketId(Market market) => market == 0 ? 0 : Context.Guild.Id;

        [Command("listings"), Alias("view", "l", "v"), Summary("Views market listings in the global market")]
        public async Task ViewListings(string emoji = "all", [Remainder] string sorting = "lowest") => await ViewListings(Market.Global, emoji, sorting);

        [Command("listings"), Alias("view", "l", "v"), Summary("Views market listings in either the global or local markets")]
        public async Task ViewListings(Market m, string emoji = "all", [Remainder] string sorting = "lowest")
        {
            string[] HIGH_TO_LOW = { "highest", "highest first", "highest to lowest", "greatest to least", "expensive", "pricy", "g2l", "h2l" };
            string[] LOW_TO_HIGH = { "lowest", "lowest first", "lowest to highest", "least to greatest", "cheap", "affordable", "l2g", "l2h" };

            var market = MarketHelper.GetOrCreate(Context.MarketCollection, MarketId(m));

            IEnumerable<(string e, Listing l)> listings;
            if (emoji == "all")
            {
                listings = market.Listings.SelectMany(kv => kv.Value.Select(l => (kv.Key, l)));
                
                if (listings.Count() == 0) throw new DiscordCommandException($"There are no listings in {Context.GetMarketName(MarketId(m))}");
            }
            else
            {
                if (!EmojiHelper.IsValidEmoji(emoji)) throw new DiscordCommandException($"{emoji} cannot be bought or sold");

                if (!market.Listings.ContainsKey(emoji)) throw new DiscordCommandException($"There are no listings for {emoji} in {Context.GetMarketName(MarketId(m))}");

                listings = market.Listings[emoji].Select(l => (emoji, l));
            }

            if (HIGH_TO_LOW.Contains(sorting.Trim()))
            {
                listings = listings.OrderByDescending(p => p.l.Price);
            }
            else
            {
                listings = listings.OrderBy(p => p.l.Price);
            }

            string title = $"{(emoji == "all" ? "All listings" : emoji)} in {Context.GetMarketName(MarketId(m))}";

            const int NUM_PER_PAGE = 10;

            int totalPages = (listings.Count() + NUM_PER_PAGE - 1) / NUM_PER_PAGE;

            Embed getPage(int page)
            {
                EmbedBuilder builder = new EmbedBuilder();
                StringBuilder stringBuilder = new StringBuilder();
                List<string> contents = new List<string>();
                foreach (var (e, listing) in listings.Skip((page - 1) * NUM_PER_PAGE).Take(NUM_PER_PAGE))
                {
                    stringBuilder.Append($"{e} for {Context.Money(listing.Price)} by {(m == Market.G ? Context.Bot.Client.GetUser(listing.SellerId).ToString() : Context.WhatDoICall(listing.SellerId))}\n");
                }

                builder.AddField(new EmbedFieldBuilder().WithName(title).WithValue(stringBuilder.ToString()));
                builder.Footer = new EmbedFooterBuilder().WithText($"Page {page} of {totalPages}");

                return builder.Build();
            }

            var message = await ReplyAsync(embed: getPage(1));

            ReactionMessageHelper.CreatePaginatedMessage(Context, message, totalPages, 1, pg => Task.FromResult(("", getPage(pg.CurrentPage))));
        }

        [Command("buy"), Alias("purchase", "order", "b"), Summary("Purchase an emoji from the global market")]
        public async Task BuyEmoji(string emoji) => await BuyEmoji(Market.Global, emoji);

        [Command("buy"), Alias("purchase", "order", "b"), Summary("Purchase an emoji from either the global or local markets")]
        public async Task BuyEmoji(Market market, string emoji)
        {
            if (!EmojiHelper.IsValidEmoji(emoji)) throw new DiscordCommandException($"{emoji} cannot be bought or sold");

            (var price, var valid) = MarketHelper.GetEmojiPrice(Context, MarketId(market), emoji);

            if (!valid) throw new DiscordCommandException($"There are no listings for {emoji} in {Context.GetMarketName(MarketId(market))}");

            int money = Context.CallerProfile.Currency;

            if (price > money) throw new DiscordCommandException($"Sorry, {Context.WhatDoICall(Context.User)}, you need {Context.Money(price - money)} more to buy {emoji}");

            MarketHelper.BuyListing(Context, MarketId(market), emoji, Context.User.Id);
        }

        [Command("sell"), Alias("offer", "s"), Summary("Put one of your emoji up for sale on the global market")]
        public async Task SellEmoji(string emoji, int price) => await SellEmoji(Market.Global, emoji, price);

        [Command("sell"), Alias("offer", "s"), Summary("Put one of your emoji up for sale in either the global or local markets")]
        public async Task SellEmoji(Market market, string emoji, int price)
        {
            if (!EmojiHelper.IsValidEmoji(emoji)) throw new DiscordCommandException($"{emoji} cannot be bought or sold");

            var inventory = Context.GetInventory(Context.CallerProfile);

            if (!inventory.HasEmoji(emoji)) throw new DiscordCommandException($"{Context.User.Mention}, you don't have any {emoji} to sell");

            if (price <= 0) throw new DiscordCommandException($"{Context.User.Mention}, you can't sell things for {(price == 0 ? "" : "less than ")}no money");

            var message = await ReplyAsync($"{Context.User.Mention}, are you sure you want to sell {emoji} for {Context.Money(price)}?");
            ReactionMessageHelper.CreateConfirmReactionMessage(Context, message,
                async r =>
                {
                    var (toSell, index) = inventory.Enumerate(emoji).Select((e, i) => (e, i)).OrderBy(e => e.e.Transactions.Count).First();

                    inventory.RemoveEmoji(emoji, index);

                    MarketHelper.AddListing(Context, MarketId(market), toSell, price);
                    inventory.Save();
                    await message.ModifyAsync(m => m.Content = $"Listed your {emoji} for {Context.Money(price)} in {Context.GetMarketName(MarketId(market))}");
                }, 
                async r =>
                {
                    await message.ModifyAsync(m => m.Content = $"Cancelled listing of {emoji}");
                });
        }
    }
}
