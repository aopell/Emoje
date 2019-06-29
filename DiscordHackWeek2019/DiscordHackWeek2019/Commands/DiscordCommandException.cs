using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Commands
{
    public class DiscordCommandException : Exception
    {
        public string Title { get; set; }
        public DiscordCommandException(string message) : this("Error Executing Command", message)
        {
        }

        public DiscordCommandException(string title, string message) : base(message)
        {
            Title = title;
        }
    }
}
