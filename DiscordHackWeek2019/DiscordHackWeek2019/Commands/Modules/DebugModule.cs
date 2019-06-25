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
        public Task Offer()
        {

        }
    }
}