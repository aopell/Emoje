using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Config
{
    [ConfigFile("secret.json")]
    public class Secret : Config
    {
        public string Token { get; set; }
        public string IexCloudSecret { get; set; }
    }
}
