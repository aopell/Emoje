using Discord;
using Discord.WebSocket;
using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Helpers
{
    public static class ExceptionMessageHelper
    {
        public static async Task HandleException(Exception ex, ISocketMessageChannel channel)
        {
            EmbedBuilder embed = new EmbedBuilder();
            StackTrace trace = new StackTrace(ex, true);
            string[] fileNames = trace.GetFrame(0).GetFileName().Split('/', '\\');
            string fileName = fileNames?[fileNames.Length - 1];
            int lineNo = trace.GetFrame(0).GetFileLineNumber();

            if (ex is DiscordCommandException dex)
            {
                embed.WithTitle(dex.Title);
                embed.WithColor(Color.Red);
                embed.WithDescription(dex.Message);
#if DEBUG
                embed.WithFooter($"{ex.GetType().Name}" + (fileName == null ? "" : $" at {fileName}:{lineNo}"));
#endif
            }
            else
            {
                embed.WithTitle("Internal Error");
                embed.WithColor(Color.Red);
                embed.WithDescription($"An internal error has occurred while executing this command. More error details have been supplied below.\n\n{ex.Message}");
                embed.WithFooter($"{ex.GetType().Name}" + (fileName == null ? "" : $" at {fileName}:{lineNo}"));
            }

            await channel.SendMessageAsync(embed: embed.Build());
        }
    }
}
