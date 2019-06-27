﻿using Discord.Commands;
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
                if (!EmojiHelper.IsValidEmoji(emoji))
                {
                    await ReplyAsync($"{emoji} cannot be bought or sold.");
                    return;
                }
                // TODO: actually buy it
                await ReplyAsync("Cost: " + MarketHelper.GetEmojiPrice(Context, 0, emoji));
            }

            [Command("sell"), Alias("offer"), Summary("Put one of your emoji up for sale on the global market")]
            public async Task SellEmoji(string emoji, int price)
            {
                if (!EmojiHelper.IsValidEmoji(emoji))
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
                            Transactions = null
                        };
                        var added = MarketHelper.AddListing(Context, 0, toSell, price);
                        await ReplyAsync($"Added a listing: {emoji}: ${added.Price}");
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
