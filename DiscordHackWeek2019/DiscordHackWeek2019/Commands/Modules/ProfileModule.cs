using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    public class ProfileModule : ModuleBase<BotCommandContext>
    {
        [Command("profile"), Summary("Displays the profile of yourself or another user")]
        public Task ViewProfile([Remainder] IUser user = null)
        {
            throw new NotImplementedException();
        }

        [Command("inventory"), Summary("Displays the inventory of yourself or another user")]
        public Task ViewInventory([Remainder] IUser user = null)
        {
            throw new NotImplementedException();
        }
    }
}
