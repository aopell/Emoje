using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands
{
    public class JoinRequiredAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context is BotCommandContext botContext)
            {
                if (botContext.UserJoined(botContext.User.Id))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }

                return Task.FromResult(PreconditionResult.FromError(Strings.UserJoinNeeded));
            }

            return Task.FromResult(PreconditionResult.FromError(new InvalidOperationException()));
        }
    }
}
