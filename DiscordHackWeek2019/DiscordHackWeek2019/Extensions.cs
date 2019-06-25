using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019
{
    public static class Extensions
    {
        public static T GetById<T>(this LiteCollection<T> c, ulong id)
        {
            return c.FindById((long)id);
        }
    }
}
