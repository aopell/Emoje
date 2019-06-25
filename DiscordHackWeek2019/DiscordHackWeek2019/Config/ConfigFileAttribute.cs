using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Config
{
    public class ConfigFileAttribute : Attribute
    {
        public string FileName { get; }

        public ConfigFileAttribute(string fileName)
        {
            FileName = fileName;
        }
    }
}
