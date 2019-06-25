using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("trade")]
    public class DebugModule : ModuleBase<BotCommandContext>
    {
        [Command("offer"), Alias("create"), Summary("Create a trade offer to send to another user")]
        public Task Offer()
        {
            throw new NotImplementedException();
        }

        [Command("accept"), Summary("Accept a trade offer from another player")]
        public Task Accept()
        {
            throw new NotImplementedException();
        }

        [Command("decline"), Summary("Decline a trade offer from another player")]
        public Task Decline()
        {
            throw new NotImplementedException();
        }
    }
}