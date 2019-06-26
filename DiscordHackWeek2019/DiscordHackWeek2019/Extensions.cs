using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordHackWeek2019
{
    public static class Extensions
    {
        public static T GetById<T>(this LiteCollection<T> c, ulong id)
        {
            return c.FindById((long)id);
        }

        public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest)
        {

            first = list.Count > 0 ? list[0] : default(T); // or throw
            rest = list.Skip(1).ToList();
        }

        public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest)
        {
            first = list.Count > 0 ? list[0] : default(T); // or throw
            second = list.Count > 1 ? list[1] : default(T); // or throw
            rest = list.Skip(2).ToList();
        }
        public static string AvatarUrlOrDefaultAvatar(this IUser user) => user.GetAvatarUrl() ?? $"https://cdn.discordapp.com/embed/avatars/{user.DiscriminatorValue % 5}.png";
    }
}
