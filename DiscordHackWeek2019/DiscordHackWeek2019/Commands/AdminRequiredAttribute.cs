using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands
{
    public class AdminRequiredAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context is BotCommandContext bContext && bContext.Bot.Secret.Admins != null && bContext.Bot.Secret.Admins.Length > 0)
            {
                return bContext.Bot.Secret.Admins.Contains(context.User.Id) ? Task.FromResult(PreconditionResult.FromSuccess()) : Task.FromResult(PreconditionResult.FromError("You must be an admin to run this command"));
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
