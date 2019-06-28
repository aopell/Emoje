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
            if (Context.UserJoined(Context.User.Id))
            {
                // User has already joined
                await ReplyAsync("You've already joined. There's no going back."); // TODO: real message
                return;
            }

            Context.UserCollection.Insert(new User
            {
                UserId = Context.User.Id,
                Currency = Context.Bot.Options.StartingCurrency,
                CurrentInvestments = new PortfolioCollection(),
                Inventory = new Dictionary<string, List<Guid>>(),
                LootBoxes = new Dictionary<string, int>(),
                PreviousInvestments = new PortfolioCollection(),
                Transactions = new List<TransactionInfo>()
            });

            await ReplyAsync($"Welcome {Context.User.Mention}! {Context.Bot.Options.StartingCurrency} currency has been deposited to your account.");
        }

        [Command("profile"), Summary("Displays the profile of yourself or another user"), JoinRequired]
        public async Task ViewProfile([Remainder] IUser user = null)
        {
            user = Context.GetUserOrSender(user);
            var profile = Context.GetProfile(user);

            var embed = Context.EmbedFromUser(user);
            embed.WithTitle("Profile");
            embed.AddField("Currency", $"{Strings.moneyEmoji} " + profile.Currency, true);
            embed.AddField("Unique Emoji", $"{Strings.emojiEmoji} " + profile.Inventory.Count, true);
            embed.AddField("Owned Loot Boxes", $"{Strings.boxEmoji} " + profile.LootBoxes.Count, true);
            embed.AddField("Transactions Completed", $"{Strings.transactionEmoji} " + profile.Transactions.Count, true);
            embed.AddField("Unique Stocks", $"{Strings.stockEmoji} " + profile.CurrentInvestments.Stocks.Items.Values.Count(x => x.Count > 0), true);
            embed.AddField("Unique Cryptocurrencies", $"{Strings.cryptoEmoji} " + profile.CurrentInvestments.Crypto.Items.Values.Count(x => x.Count > 0), true);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("inventory"), Alias("i", "inv"), Summary("Displays the inventory of yourself or another user"), JoinRequired]
        public async Task ViewInventory([Remainder] IUser user = null)
        {
            // Get a list of emojis
            user = Context.GetUserOrSender(user);
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
                await ReplyAsync("You don't have any emojis!!!");
                return;
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
                return Task.FromResult(($"", embeds[m.CurrentPage -1].Build()));
            });
        }

        [Command("details"), Summary("View all of one emoji you own"), JoinRequired]
        public async Task ViewInventory(string emoji)
        {
            if (!Helpers.EmojiHelper.IsValidEmoji(emoji))
            {
                await ReplyAsync("That is not a valid emoji");
                return;
            }

            // Get a list of emojis
            Helpers.InventoryWrapper inventory = new Helpers.InventoryWrapper(Context, Context.User.Id);
            var emojis = inventory.Enumerate(emoji);

            List<string> contents = new List<string>();
            foreach (var e in emojis)
            {
                contents.Add($"{e.Unicode}: {e.EmojiId}");
            }

            var embeds = Helpers.EmbedHelper.MakeEmbeds(Context, contents, "Details:", 15);

            var message = await ReplyAsync(embed: embeds[0].Build());
            Helpers.ReactionMessageHelper.CreatePaginatedMessage(Context, message, embeds.Count, 1, m =>
            {
                return Task.FromResult(($"", embeds[m.CurrentPage - 1].Build()));
            });
        }
    }
}
