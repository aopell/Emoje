using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Config
{
    [ConfigFile("Config/options.json")]
    public class Options : Config
    {
        public long StartingCurrency { get; set; }
    }
}
