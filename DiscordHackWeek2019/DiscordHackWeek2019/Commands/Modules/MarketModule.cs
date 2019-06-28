using Discord.Commands;
using DiscordHackWeek2019.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("market"), JoinRequired]
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

        [Command("listings"), Alias("view"), Summary("Views market listings in the global market")]
        public Task ViewListings() => ViewListings(Market.Global);

        [Command("listings"), Alias("view"), Summary("Views market listings in either the global or local markets")]
        public async Task ViewListings(Market market, string emoji = "all", [Remainder] string sorting = "lowest")
        {
            switch (sorting)
            {
                case "highest":
                case "highest to lowest":
                case "greatest to least":
                case "g2l":
                case "h2l":
                    break;
                case "lowest":
                case "lowest to highest":
                case "lowest to ":
                case "cheap":
                case "l2g":
                case "l2h":
                default:

                    break;
            }
        }

        [Command("buy"), Alias("purchase", "order"), Summary("Purchase an emoji from the global market")]
        public async Task BuyEmoji(string emoji) => BuyEmoji(Market.Global, emoji);

        [Command("buy"), Alias("purchase", "order"), Summary("Purchase an emoji from either the global or local markets")]
        public async Task BuyEmoji(Market market, string emoji)
        {
            if (!EmojiHelper.IsValidEmoji(emoji))
            {
                await ReplyAsync($"{emoji} cannot be bought or sold");
                return;
            }

            (var price, var valid) = MarketHelper.GetEmojiPrice(Context, MarketId(market), emoji);

            if (!valid)
            {
                await ReplyAsync($"There are no listings for {emoji} in {Context.GetMarketName(MarketId(market))}");
                return;
            }

            int money = Context.CallerProfile.Currency;

            if (price > money)
            {
                await ReplyAsync($"Sorry, {Context.WhatDoICall(Context.User)}, you need {Context.Money(price - money)} more to buy {emoji}");
                return;
            }

            MarketHelper.BuyListing(Context, MarketId(market), emoji, Context.User.Id);
        }

        [Command("sell"), Alias("offer"), Summary("Put one of your emoji up for sale on the global market")]
        public async Task SellEmoji(string emoji, int price) => SellEmoji(Market.Global, emoji, price);

        [Command("sell"), Alias("offer"), Summary("Put one of your emoji up for sale in either the global or local markets")]
        public async Task SellEmoji(Market market, string emoji, int price)
        {
            if (!EmojiHelper.IsValidEmoji(emoji))
            {
                await ReplyAsync($"{emoji} cannot be bought or sold");
                return;
            }

            var inventory = Context.GetInventory(Context.CallerProfile);

            if (!inventory.HasEmoji(emoji))
            {
                await ReplyAsync($"{Context.User.Mention}, you don't have any {emoji} to sell");
                return;
            }

            if (price <= 0)
            {
                await ReplyAsync($"{Context.User.Mention}, you can't sell things for {(price == 0 ? "" : "less than ")} no money");
                return;
            }

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
