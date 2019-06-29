using Discord;
using Discord.Commands;
using DiscordHackWeek2019.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("debug")]
    public class DebugModule : ModuleBase<BotCommandContext>
    {
        [Command("emojilist")]
        public async Task EmojiList()
        {
            var sb = new StringBuilder();
            foreach (var emoji in EmojiHelper.IterateAllEmoji)
            {
                sb.Append(emoji);
            }
            await ReplyAsync(sb.ToString());
        }

        [Command("confirm")]
        public async Task ConfirmMessage()
        {
            var message = await ReplyAsync("Test confirm message");
            ReactionMessageHelper.CreateConfirmReactionMessage(Context, message, async r => await ReplyAsync("Positive response received"), async r => await ReplyAsync("Negative response received"));
        }

        [Command("custom")]
        public async Task CustomReactions([Remainder] string input)
        {
            var emoji = input.Split(' ');
            var message = await ReplyAsync("Select a reaction");
            ReactionMessageHelper.CreateReactionMessage(Context, message, emoji.Select(x => new KeyValuePair<string, Func<ReactionMessage, Task>>(x, async r => await ReplyAsync($"{x} selected"))).ToDictionary(x => x.Key, x => x.Value));
        }

        [Command("multicustom")]
        public async Task MultiCustom([Remainder] string input)
        {
            var emoji = input.Split(' ');
            var message = await ReplyAsync("Select one or more reactions");
            ReactionMessageHelper.CreateReactionMessage(Context, message, emoji.Select(x => new KeyValuePair<string, Func<ReactionMessage, Task>>(x, async r => await ReplyAsync($"{x} selected"))).ToDictionary(x => x.Key, x => x.Value), true);
        }

        [Command("repeat")]
        public async Task ReactionMessage()
        {
            var message = await ReplyAsync("Test generic reaction message");
            ReactionMessageHelper.CreateCustomReactionMessage(Context, message, async (r, s) => await ReplyAsync($"Received reaction {s}"));
        }

        [Command("multirepeat")]
        public async Task MultiReactionMessage()
        {
            var message = await ReplyAsync("Test multi generic reaction message");
            ReactionMessageHelper.CreateCustomReactionMessage(Context, message, async (r, s) => await ReplyAsync($"[Multi] Received reaction {s}"), true);
        }

        [Command("rarities")]
        public async Task RaritiesMessage()
        {
            string message;
            var emotes = Helpers.MarketHelper.GetRarities(Context.Bot, 0);
            foreach (var rarity in Rarity.Rarities)
            {
                message = $"```\n{rarity.Label}:\n";
                foreach (var e in emotes[rarity])
                {
                    message += new Discord.Emoji(e);
                }
                message += "\n```";
                await ReplyAsync(message);
            }
        }

        [Command("pages")]
        public async Task Pages()
        {
            var message = await ReplyAsync("Page 1");
            ReactionMessageHelper.CreatePaginatedMessage(Context, message, 10, 1, m =>
            {
                return Task.FromResult(($"Page {m.CurrentPage}", (Embed)null));
            });
        }

        [Command("currency"), Alias("money")]
        public async Task SetCurrency(Discord.IUser user, int amount)
        {
            var thing = Context.UserCollection.GetById(user.Id);
            thing.Currency = amount;
            Context.UserCollection.Update(thing);
            await ReplyAsync("Ok!");
        }

        [Command("give"), Alias("emoji", "emote")]
        public async Task GiveEmoji(Discord.IUser user, string emoji, int amount = 1)
        {
            var thing = Context.GetInventory(user);
            amount = amount <= 25 ? amount : 25;

            for (int i = 0; i < amount; i++)
            {
                var trans = Models.Transaction.FromLootbox(marketId: 0, buyer: thing.UserId, "Admin");
                thing.Add(new Models.Emoji
                {
                    Unicode = emoji,
                    Transactions = new List<Models.TransactionInfo>() { Context.Bot.Clerk.Queue(trans).Receive() },
                }, true);
            }

            thing.Save();
            await ReplyAsync("Ok!");
        }

        [Command("lootbox"), Alias("loot", "lootboxes", "box")]
        public async Task SetLootboxes(Discord.IUser user, int amount)
        {
            var inventory = new InventoryWrapper(Context, user.Id);
            inventory.AddLoot("normal", amount);
            inventory.Save();

            await ReplyAsync("Ok!");
        }
    }
}