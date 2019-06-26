using Discord.Commands;
using System;
using System.Collections.Generic;
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
    }
}