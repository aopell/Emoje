using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using DiscordHackWeek2019.Commands;

namespace DiscordHackWeek2019.Helpers
{
    public static class EmbedHelper
    {
        public static List<EmbedBuilder> MakeEmbeds(BotCommandContext context, List<string> contents, string label, int maxLines)
        {
            List<EmbedBuilder> res = new List<EmbedBuilder>();
            if (contents.Count == 0)
            {
                return res;
            }

            int count = 0;
            StringBuilder builder = new StringBuilder();

            foreach (string line in contents)
            {
                builder.Append(line + "\n");
                count++;
                if (count == maxLines)
                {
                    var embed = context.EmbedFromUser(context.User);
                    embed.AddField($"{label}", builder.ToString(), true);
                    res.Add(embed);
                    builder.Clear();
                    count = 0;
                }
            }
            var embedd = context.EmbedFromUser(context.User);
            if (builder.Length != 0)
            {
                embedd.AddField($"{label}", builder.ToString(), true);
                res.Add(embedd);
            }

            for(int i = 0; i < res.Count; i++)
            {
                res[i].WithFooter($"Page {i + 1} of {res.Count}");
            }

            return res;
        }
    }
}
