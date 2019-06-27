using Discord;
using Discord.Commands;
using DiscordHackWeek2019.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace DiscordHackWeek2019.Commands.Modules
{
    public class GamblingModule : ModuleBase<BotCommandContext>
    {
        [Group("lootbox"), Alias("box", "lootboxes"), JoinRequired]
        public class LootBoxModule : ModuleBase<BotCommandContext>
        {
            [Command("buy"), Alias("purchase"), Summary("Buy one or more lootboxes")]
            public async Task Buy(int count = 1, string type = null)
            {
                if (!Context.UserJoined(Context.User.Id))
                {
                    await ReplyAsync(Strings.UserJoinNeeded);
                    return;
                }

                var allEmoji = Context.Bot.EmojiHelper.IterateAllEmoji;
                int size = allEmoji.Count();

                var inventory = Context.GetInventory(Context.User);

                var awaitList = new List<Task>();

                for (int i = 0; i < 4; i++)
                {
                    string emoji = allEmoji.ElementAt(Context.Bot.Random.Next(size - 1));

                    awaitList.Add(ReplyAsync(emoji));

                    inventory.Add(new Models.Emoji { Unicode = emoji, Transactions = new List<TransactionInfo>() }, true);
                }

                Task.WaitAll(awaitList.ToArray());

                inventory.Save();
            }

            [Command("open"), Summary("Open a lootbox")]
            public Task Open(string type = null)
            {
                throw new NotImplementedException();
            }

            [Command("view"), Summary("View your currently owned loot boxes")]
            public Task View()
            {
                throw new NotImplementedException();
            }
        }
    }
}
