using Discord;
using Discord.Commands;
using DiscordHackWeek2019.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DiscordHackWeek2019.Helpers;

namespace DiscordHackWeek2019.Commands.Modules
{
    public class GamblingModule : ModuleBase<BotCommandContext>
    {
        [Group("lootbox"), Alias("box", "lootboxes"), JoinRequired]
        public class LootBoxModule : ModuleBase<BotCommandContext>
        {
            [Command("buy"), Alias("purchase"), Summary("Buy one or more lootboxes, opening it instantly")]
            public async Task Buy(int count = 1, string type = "normal")
            {
                // Limit count
                if(count > 5 || count < 1)
                {
                    await ReplyAsync("You can only open up to 5 loot boxes at a time.");
                    return;
                }

                StringBuilder message = new StringBuilder();
                var inventory = Context.GetInventory(Context.User);

                for (int i = 0; i < count; i++)
                {
                    var result = LootBoxHelper.LootBoxVarieties[type].Open(Context.Bot, 0);

                    foreach (var (rarity, emoji) in result)
                    {
                        // TODO: Add transaction showing this is from a lootbox
                        inventory.Add(new Models.Emoji { Owner = Context.User.Id, Transactions = new List<TransactionInfo>() { }, Unicode = emoji });
                        message.Append($"{rarity.LeftBracket}{emoji}{rarity.RightBracket} ");
                    }
                    message.AppendLine();
                }

                inventory.Save();

                await ReplyAsync(message.ToString());
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
