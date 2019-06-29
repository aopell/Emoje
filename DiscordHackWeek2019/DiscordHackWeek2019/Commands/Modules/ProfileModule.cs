using Discord;
using Discord.Commands;
using DiscordHackWeek2019.Models;
using DiscordHackWeek2019.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using System.Linq;

namespace DiscordHackWeek2019.Commands.Modules
{
    public class ProfileModule : ModuleBase<BotCommandContext>
    {
        [Command("join"), Alias("optin", "optout"), Summary("Sell your soul. For a modest amount of currency.")]
        public async Task Join()
        {
            if (Context.UserJoined(Context.User.Id)) throw new DiscordCommandException("Already joined", "You've already joined. There's no going back."); // TODO: real message

            Context.UserCollection.Insert(new User
            {
                UserId = Context.User.Id,
                Currency = Context.Bot.Options.StartingCurrency,
                Investments = new PortfolioCollection(),
                Inventory = new Dictionary<string, List<Guid>>(),
                LootBoxes = new Dictionary<string, int>(),
                Transactions = new List<TransactionInfo>()
            });

            await ReplyAsync($"Welcome {Context.User.Mention}! You will find that {Context.Money(Context.Bot.Options.StartingCurrency)} has been deposited into your account.");
        }

        [Command("profile"), Summary("Displays the profile of yourself or another user")]
        public async Task ViewProfile([Remainder, Summary("The user whose profile you want to see, defaults to you")] IUser user = null)
        {
            bool current = user == null;
            user = Context.GetUserOrSender(user);
            if (!Context.UserJoined(user.Id)) throw new DiscordCommandException(current ? "You haven't joined" : "User not found", current ? Strings.UserJoinNeeded(user) : "That user has not joined the Emojeconomy");
            var profile = Context.GetProfile(user);

            var embed = Context.EmbedFromUser(user);
            embed.WithTitle("Profile");
            embed.AddField("Currency", $"{Strings.moneyEmoji} " + profile.Currency, true);
            embed.AddField("Unique Emoji", $"{Strings.emojiEmoji} " + $"{profile.Inventory.Count}/{Helpers.EmojiHelper.IterateAllEmoji.Count}", true);
            embed.AddField("Owned Loot Boxes", $"{Strings.boxEmoji} " + profile.LootBoxes.Sum(kv => kv.Value), true);
            embed.AddField("Transactions Completed", $"{Strings.transactionEmoji} " + profile.Transactions.Count, true);
            embed.AddField("Unique Stocks", $"{Strings.stockEmoji} " + profile.Investments.Stocks.Active.Values.Count(x => x.Count > 0), true);
            embed.AddField("Unique Cryptocurrencies", $"{Strings.cryptoEmoji} " + profile.Investments.Crypto.Active.Values.Count(x => x.Count > 0), true);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("inventory"), Alias("i", "inv"), Summary("Displays the inventory of yourself or another user")]
        public async Task ViewInventory([Remainder, Summary("The user whose inventory you want to see, defaults to you")] IUser user = null)
        {
            // Get a list of emojis
            bool current = user == null;
            user = Context.GetUserOrSender(user);
            if (!Context.UserJoined(user.Id)) throw new DiscordCommandException(current ? "You haven't joined" : "User not found", current ? Strings.UserJoinNeeded(user) : "That user has not joined the Emojeconomy");
            Helpers.InventoryWrapper inventory = new Helpers.InventoryWrapper(Context, user.Id);
            var emojis = inventory.Enumerate();

            // Count them
            StringBuilder emojiList = new StringBuilder();
            Dictionary<string, int> emojisCount = new Dictionary<string, int>();
            if (emojis != null)
            {
                foreach (var emoji in emojis)
                {
                    if (!emojisCount.Keys.Contains(emoji.Unicode))
                    {
                        emojisCount[emoji.Unicode] = 0;
                    }
                    emojisCount[emoji.Unicode]++;
                }
            }
            else
            {
                return;
            }

            if (emojisCount.Keys.Count == 0)
            {
                throw new DiscordCommandException("Empty inventory", "You don't have any emojis. Go get some!!!");
            }

            int inLineCount = 1;
            StringBuilder line = new StringBuilder();
            List<string> contents = new List<string>();
            foreach (string key in Helpers.EmojiHelper.IterateAllEmojiOrdered)
            {
                if (emojisCount.Keys.Contains(key))
                {
                    line.Append($"{key} x `{emojisCount[key]:000}`   ");
                    if (inLineCount == 5)
                    {
                        contents.Add(line.ToString());
                        line.Clear();
                        inLineCount = 0;
                    }
                    inLineCount++;
                }
            }
            if (line.Length != 0)
            {
                contents.Add(line.ToString());
            }

            var embeds = Helpers.EmbedHelper.MakeEmbeds(Context, contents, "Emojis:", 8);

            var message = await ReplyAsync(embed: embeds[0].Build());
            Helpers.ReactionMessageHelper.CreatePaginatedMessage(Context, message, embeds.Count, 1, m =>
            {
                return Task.FromResult(($"", embeds[m.CurrentPage - 1].Build()));
            });
        }

        [Command("details"), Summary("See more details about emoji you own"), JoinRequired]
        public async Task ViewDetails([Summary("The emoji you want details of")] string emoji)
        {
            if (!Helpers.EmojiHelper.IsValidEmoji(emoji)) throw new DiscordCommandException("Bad emoji", $"{emoji} cannot be bought, sold, or owned");

            // Get a list of emojis
            Helpers.InventoryWrapper inventory = Context.GetInventory(Context.User.Id);
            IEnumerable<Models.Emoji> emojis;
            try
            {
                emojis = inventory.Enumerate(emoji);
            }
            catch
            {
                throw new DiscordCommandException("Bad emoji", $"You do not have anything that matches {emoji}");
            }

            List<string> contents = new List<string>();
            var emojisSort = emojis.OrderByDescending(e => e.Transactions.Count);
            foreach (var e in emojisSort)
            {
                contents.Add($"{e.Unicode}: {e.EmojiId} ({e.Transactions.Count})");
            }

            var embeds = Helpers.EmbedHelper.MakeEmbeds(Context, contents, "Emoji: ID (# transactions)", 15);
            if (embeds.Count == 0)
            {
                throw new DiscordCommandException("Bad emoji", $"You do not have anything that matches {emoji}");
            }

            var message = await ReplyAsync(embed: embeds[0].Build());
            Helpers.ReactionMessageHelper.CreatePaginatedMessage(Context, message, embeds.Count, 1, m =>
            {
                return Task.FromResult(($"", embeds[m.CurrentPage - 1].Build()));
            });
        }

        [Command("history"), Summary("See more details about an emoji"), JoinRequired]
        public async Task ViewHistory([Summary("The id of the emoji you want the history of. Try getting an emoji id with `+details`")] string id)
        {
            Guid guid;
            try
            {
                guid = Guid.Parse(id);
            }
            catch
            {
                throw new DiscordCommandException("Bad id", $"Could not parse that id. Try getting an emoji id with `+details`.");
            }

            Models.Emoji emoji = Context.Bot.DataProvider.GetCollection<Models.Emoji>("emoji").FindById(guid);

            if (emoji == null)
            {
                throw new DiscordCommandException("False id", $"Could not find an emoji with that id.");
            }

            List<TransactionInfo> transactions = emoji.Transactions;
            List<string> contents = new List<string>();

            foreach (var t in transactions)
            {
                var market = Context.Bot.DataProvider.GetCollection<Models.Market>("markets").GetById(t.MarketId);
                //var tInfo = market.Transactions.OrderBy(e => e.TransactionId).FirstOrDefault();
                var tInfo = market.Transactions[(int)t.TransactionId];
                if (tInfo == null)
                {
                    continue;
                }

                if (tInfo.Data.GetType() == typeof(FromLootbox))
                {
                    var data = (FromLootbox)tInfo.Data;
                    var buyer = DiscordBot.MainInstance.Client.GetUser(data.Acquisitor);
                    contents.Add($"{buyer.Username}#{buyer.Discriminator} received from a {data.LootboxType} lootbox");
                }
                else if (tInfo.Data.GetType() == typeof(BetweenUsers))
                {
                    var data = (BetweenUsers)tInfo.Data;
                    var buyer = DiscordBot.MainInstance.Client.GetUser(data.Acquisitor);
                    contents.Add($"{buyer.Username}#{buyer.Discriminator} bought for e̩̍{data.Price}");
                }
            }

            var embeds = Helpers.EmbedHelper.MakeEmbeds(Context, contents, $"{emoji.Unicode} - {guid.ToString()}", 15);
            if (embeds.Count == 0)
            {
                throw new DiscordCommandException("Nothing to show", $"This {emoji.Unicode} has no history");
            }

            var message = await ReplyAsync(embed: embeds[0].Build());
            Helpers.ReactionMessageHelper.CreatePaginatedMessage(Context, message, embeds.Count, 1, m =>
            {
                return Task.FromResult(($"", embeds[m.CurrentPage - 1].Build()));
            });
        }
    }
}
