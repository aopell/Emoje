using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("market")]
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
            public async Task BuyEmoji(string thing)
            {
                // TODO: actually buy it
                await ReplyAsync("Cost: " + Helpers.MarketHelper.getEmojiPrice(Context, 0, thing));
            }

            [Command("sell"), Alias("offer"), Summary("Put one of your emoji up for sale on the global market")]
            public async Task SellEmoji(string emoji, float price)
            {
                if (price <= 0)
                {
                    await ReplyAsync("Please enter in a valid price");
                    return;
                }

                // TODO: get this data from the inventory and remove it from there
                Models.Emoji toSell = new Models.Emoji
                {
                    EmojiId = 4,
                    Unicode = emoji,
                    Owner = Context.User.Id,
                    Transactions = null
                };
                var added = Helpers.MarketHelper.addListing(Context, 0, toSell, price);
                await ReplyAsync($"Added a listing selling {emoji} for ${added.Price}");
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
