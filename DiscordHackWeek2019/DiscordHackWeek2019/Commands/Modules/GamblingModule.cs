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
                var availableVarieties = LootBoxHelper.GetAllLootBoxNames(Context.Guild.Id);
                if (!availableVarieties.Contains(type))
                {
                    await ReplyAsync($"{type} isn't a lootbox you can buy, try one of these:\n{string.Join(", ", availableVarieties)}");
                    return;
                }

                var variety = LootBoxHelper.GetAllLootBoxes(Context)[type];

                var inventory = Context.GetInventory(Context.User);

                if (inventory.Currency < variety.Cost)
                {
                    await ReplyAsync($"Sorry, {Context.WhatDoICall(Context.User)}, you can't afford one.");
                    return;
                }

                int actualCount = Math.Min(inventory.Currency / variety.Cost, count);
                int cost = actualCount * variety.Cost;

                string text;

                if (count > actualCount) text = $"{Context.User.Mention}, you can only afford {(actualCount == 1 ? "one" : actualCount.ToString())} box{(actualCount == 1 ? "" : "es")}, do you still want to purchase {(actualCount == 1 ? "it" : "them")} for {cost}?";
                else text = $"{Context.User.Mention}, are you sure you want to buy {actualCount} box{(actualCount == 1 ? "" : "es")} for {cost}?";

                var message = await Context.Channel.SendMessageAsync(text);
                ReactionMessageHelper.CreateReactionMessage(Context, message,
                    async onOkay =>
                    {
                        var modify = message.ModifyAsync(m => m.Content = $"{Context.WhatDoICall(Context.User)}, bought {actualCount} box{(actualCount == 1 ? "" : "es")} for {cost}");
                        inventory.Currency -= cost;
                        inventory.AddLootbox(type, actualCount);

                        inventory.Save();
                        await modify;
                    },
                    async onDecline =>
                    {
                        await message.ModifyAsync(m => m.Content = $"Lootbox purchase cancelled");
                    });
            }

            [Command("open"), Summary("Open a lootbox")]
            public async Task Open(int count = 1, string type = "normal")
            {
                // Limit count
                if (count > 5 || count < 1)
                {
                    await ReplyAsync("You can only open up to 5 loot boxes at a time.");
                    return;
                }

                StringBuilder message = new StringBuilder();
                var inventory = Context.GetInventory(Context.User);

                var variety = LootBoxHelper.GetAllLootBoxes(Context)[type];

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

            [Command("view"), Summary("View your currently owned loot boxes")]
            public Task View()
            {
                throw new NotImplementedException();
            }
        }
    }
}
