using Discord;
using Discord.Commands;
using DiscordHackWeek2019.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    public class GamblingModule : ModuleBase<BotCommandContext>
    {
        [Group("lootbox"), Alias("box", "lootboxes")]
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

                var user = Context.CallerProfile;

                long id = Context.EmojiCollection.Insert(new Models.Emoji()
                {
                    Unicode = "",
                    Owner = Context.User.Id,
                    Transactions = new List<TransactionInfo>()
                });
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
