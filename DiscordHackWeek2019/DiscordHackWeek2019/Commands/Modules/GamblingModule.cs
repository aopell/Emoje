using Discord;
using Discord.Commands;
using DiscordHackWeek2019.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DiscordHackWeek2019.Helpers;
using System.Threading.Tasks.Dataflow;

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
                if (count > 5 || count < 1)
                {
                    await ReplyAsync("You can only open up to 5 loot boxes at a time.");
                    return;
                }

                StringBuilder message = new StringBuilder();
                var inventory = Context.GetInventory(Context.User);

                var variety = LootBoxHelper.LootBoxVarieties[type];

                for (int i = 0; i < count; i++)
                {
                    foreach (var (rarity, emoji) in variety.Open(Context.Bot, 0))
                    {
                        var trans = Transaction.FromLootbox(marketId: 0, buyer: inventory.UserId, type);

                        inventory.Add(new Models.Emoji
                        {
                            Owner = Context.User.Id,
                            Transactions = new List<TransactionInfo>() { Context.Bot.Clerk.Queue(trans).Receive() },
                            Unicode = emoji
                        });

                        message.Append($"{rarity.LeftBracket}{emoji}{rarity.RightBracket} ");
                    }
                    message.AppendLine();
                }

                inventory.Save();

                await ReplyAsync(message.ToString());
            }

            [Command("open"), Summary("Open a lootbox")]
            public async Task Open(int count = 1, string type = null)
            {
                
            }

            [Command("view"), Summary("View your currently owned loot boxes")]
            public Task View()
            {
                throw new NotImplementedException();
            }
        }
    }
}
