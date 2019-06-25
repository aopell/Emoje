using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DiscordHackWeek2019.Config
{
    public abstract class Config
    {
        private ReaderWriterLockSlim rwLock { get; }
        private string fileName => GetType().GetCustomAttribute<ConfigFileAttribute>().FileName;

        protected Config()
        {
            rwLock = new ReaderWriterLockSlim();
        }

        public void SaveConfig()
        {
            using (new WriteLock(rwLock))
            {
                File.WriteAllText(fileName, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }
    }
}
