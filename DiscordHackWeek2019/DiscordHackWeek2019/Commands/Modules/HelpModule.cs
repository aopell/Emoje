using Discord.Commands;
using DiscordHackWeek2019.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("help")]
    public class HelpModule : ModuleBase<BotCommandContext>
    {
        [Command, Summary("Displays all available commands and how to use them")]
        public async Task Help(int page = 1)
        {
            if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive and nonzero");
            const int NUM_PER_PAGE = 5;

            StringBuilder result = new StringBuilder();
            var commands = HelpHelper.AllCommands.Skip((page - 1) * NUM_PER_PAGE).Take(NUM_PER_PAGE);
            foreach (var c in commands)
            {
                result.AppendLine(c.ToString());
            }
            await ReplyAsync(result.ToString());
        }

        [Command, Summary("Displays help for a specific command")]
        public async Task Help([Remainder] string command)
        {
            var commands = HelpHelper.AllCommands.Where(c => c.Command == command || c.Command.StartsWith(command + " "));
            StringBuilder result = new StringBuilder();
            foreach (var c in commands)
            {
                result.AppendLine(c.ToString());
            }
            await ReplyAsync(result.ToString());
        }
    }
}
