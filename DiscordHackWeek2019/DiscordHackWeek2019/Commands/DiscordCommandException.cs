using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Commands
{
    public class DiscordCommandException : Exception
    {
        public DiscordCommandException(string message) : base(message)
        {
        }
    }
}
