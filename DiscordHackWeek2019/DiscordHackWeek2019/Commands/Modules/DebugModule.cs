using Discord.Commands;
using DiscordHackWeek2019.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("debug")]
    public class DebugModule : ModuleBase<BotCommandContext>
    {
        [Command("emojilist")]
        public async Task EmojiList()
        {
            var sb = new StringBuilder();
            foreach (var emoji in Context.Bot.EmojiHelper.IterateAllEmoji)
            {
                sb.Append(emoji);
            }
            await ReplyAsync(sb.ToString());
        }

        [Command("confirm")]
        public async Task ConfirmMessage()
        {
            var message = await ReplyAsync("Test confirm message");
            ReactionMessageHelper.CreateReactionMessage(Context, message, async r => await ReplyAsync("Positive response received"), async r => await ReplyAsync("Negative response received"));
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
            ReactionMessageHelper.CreateReactionMessage(Context, message, async (r, s) => await ReplyAsync($"Received reaction {s}"));
        }

        [Command("multirepeat")]
        public async Task MultiReactionMessage()
        {
            var message = await ReplyAsync("Test multi generic reaction message");
            ReactionMessageHelper.CreateReactionMessage(Context, message, async (r, s) => await ReplyAsync($"[Multi] Received reaction {s}"), true);
        }

        [Command("rarities")]
        public async Task RaritiesMessage()
        {
            string message;
            var emotes = Helpers.MarketHelper.GetRarities(Context, 0);
            foreach (var rarity in Rarity.Rarities)
            {
                message = $"{rarity.Label}:\n";
                foreach (var e in emotes[rarity])
                {
                    message += new Discord.Emoji(e);
                }
                await ReplyAsync(message);
            }
        }
    }
}