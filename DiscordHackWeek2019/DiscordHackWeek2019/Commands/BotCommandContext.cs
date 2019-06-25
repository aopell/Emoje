using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Commands
{
    public class BotCommandContext : SocketCommandContext
    {
        public DiscordBot Bot { get; set; }

        public BotCommandContext(DiscordSocketClient client, SocketUserMessage msg, DiscordBot bot) : base(client, msg)
        {
            Bot = bot;
        }
    }
}
