using Discord.Commands;
using DiscordHackWeek2019.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("market"), JoinRequired]
    public class MarketModule : ModuleBase<BotCommandContext>
    {
        [Group("global")]
        public class GlobalMarketModule : ModuleBase<BotCommandContext>
        {
            [Command("listings"), Alias("view"), Summary("Views market listings for the global market")]
            public Task ViewListings()
            {
                throw new NotImplementedException();
            }

            [Command("buy"), Alias("purchase", "order"), Summary("Purchase an emoji from the global market")]
            public async Task BuyEmoji(string emoji)
            {
                if (!Context.Bot.EmojiHelper.IsValidEmoji(emoji))
                {
                    await ReplyAsync($"{emoji} cannot be bought or sold.");
                    return;
                }

                (var price, var valid) = MarketHelper.GetEmojiPrice(Context, 0, emoji);

                if (!valid)
                {
                    await ReplyAsync($"Sorry, {emoji} is not available");
                    return;
                }

                int money = Context.CallerProfile.Currency;

                if (price > money)
                {
                    await ReplyAsync($"Sorry, {Context.WhatDoICall(Context.User)}, you need ${price - money} more to buy {emoji}");
                    return;
                }

                MarketHelper.BuyListing(Context, 0, emoji, Context.User.Id);
            }

            [Command("sell"), Alias("offer"), Summary("Put one of your emoji up for sale on the global market")]
            public async Task SellEmoji(string emoji, int price)
            {
                if (!Context.Bot.EmojiHelper.IsValidEmoji(emoji))
                {
                    await ReplyAsync($"{emoji} cannot be bought or sold.");
                    return;
                }

                if (price <= 0)
                {
                    await ReplyAsync("Please enter in a valid price");
                    return;
                }

                var message = await ReplyAsync($"Are you sure you want to sell {emoji} for ${price}?");
                ReactionMessageHelper.CreateReactionMessage(Context, message,
                    async r =>
                    {
                        // TODO: get this data from the inventory and remove it from there
                        Models.Emoji toSell = new Models.Emoji
                        {
                            Unicode = emoji,
                            Owner = Context.User.Id,
                            Transactions = new List<Models.TransactionInfo>()
                        };

                        toSell.EmojiId = Context.EmojiCollection.Insert(toSell);

                        MarketHelper.AddListing(Context, 0, toSell, price);
                        await ReplyAsync($"Added a listing: {emoji}: ${price}");
                    }, r => Task.CompletedTask);
            }
        }

        [Group("server")]
        public class ServerMarketModule : ModuleBase<BotCommandContext>
        {
            [Command("listings"), Alias("view"), Summary("Views market listings for this server's market")]
            public Task ViewListings()
            {
                throw new NotImplementedException();
            }

            [Command("buy"), Alias("purchase", "order"), Summary("Purchase an emoji from this server's market")]
            public Task BuyEmoji()
            {
                throw new NotImplementedException();
            }

            [Command("sell"), Alias("offer"), Summary("Put one of your emoji up for sale on this server's market")]
            public Task SellEmoji()
            {
                throw new NotImplementedException();
            }
        }
    }
}
