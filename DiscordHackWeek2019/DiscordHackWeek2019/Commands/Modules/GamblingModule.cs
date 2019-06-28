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
            [Command("buy"), Alias("purchase"), Summary("Buy one or more lootboxes")]
            public async Task Buy(int count = 1, string type = "normal")
            {
                var availableVarieties = LootBoxHelper.GetAllLootBoxNames(Context.Guild.Id);
                if (!availableVarieties.Contains(type))
                {
                    await ReplyAsync($"{type} isn't a lootbox you can buy, try {(availableVarieties.Count == 1 ? "" : "one of these:\n")}{string.Join(", ", availableVarieties)}");
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

                if (count > actualCount) text = $"{Context.User.Mention}, you can only afford {(actualCount == 1 ? "one" : actualCount.ToString())} box{(actualCount == 1 ? "" : "es")}, do you still want to purchase {(actualCount == 1 ? "it" : "them")} for {Context.Money(cost)}?";
                else text = $"{Context.User.Mention}, are you sure you want to buy {(actualCount == 1 ? "one" : actualCount.ToString())} box{(actualCount == 1 ? "" : "es")} for {Context.Money(cost)}?";

                var message = await Context.Channel.SendMessageAsync(text);
                ReactionMessageHelper.CreateConfirmReactionMessage(Context, message,
                    async onOkay =>
                    {
                        var modify = message.ModifyAsync(m => m.Content = $"{Context.WhatDoICall(Context.User)}, bought {(actualCount == 1 ? "one" : actualCount.ToString())} box{(actualCount == 1 ? "" : "es")} for {Context.Money(cost)}");
                        inventory.Currency -= cost;
                        inventory.AddLoot(type, actualCount);

                        inventory.Save();
                        await modify;
                    },
                    async onDecline =>
                    {
                        await message.ModifyAsync(m => m.Content = $"Lootbox purchase cancelled");
                    });
            }

            [Command("open"), Summary("Open one or more lootboxes, buying them if you don't own enough")]
            public async Task Open(int count = 1, string type = "normal")
            {
                var availableVarieties = LootBoxHelper.GetAllLootBoxNames(Context.Guild.Id);
                if (!availableVarieties.Contains(type))
                {
                    await ReplyAsync($"{type} isn't a lootbox you can buy, try {(availableVarieties.Count == 1 ? "" : "one of these:\n")}{string.Join(", ", availableVarieties)}");
                    return;
                }

                if (count > 5)
                {
                    count = 5;
                    await ReplyAsync("You can only open 5 boxes at a time!");
                }

                var inventory = Context.GetInventory(Context.User);
                var variety = LootBoxHelper.GetAllLootBoxes(Context)[type];

                int available = inventory.GetNumLootBoxes(type);

                if (available >= count)
                {
                    inventory.RemoveBoxes(type, count);
                    var open = Open(inventory, variety, count);
                    
                    return;
                }

                if (available == 0 && inventory.Currency < variety.Cost)
                {
                    await ReplyAsync($"Sorry, {Context.WhatDoICall(Context.User)}, you can't afford one");
                    return;
                }

                int needToBuy = count - available;

                int canBuy = Math.Min(inventory.Currency / variety.Cost, needToBuy);
                int cost = canBuy * variety.Cost;

                int toOpen = available + canBuy;

                string text;

                if (needToBuy > canBuy) text = $"{Context.User.Mention}, you {(available == 0 ? "don't have any" : $"have {available}")} right now, and you can only afford {(canBuy == 1 ? "one" : canBuy.ToString())} box{(canBuy == 1 ? "" : "es")}, do you still want to purchase {(canBuy == 1 ? "it" : "them")} for {Context.Money(cost)} and open {(toOpen == 1 ? "one" : toOpen.ToString())}?";
                else text = $"{Context.User.Mention}, you {(available == 0 ? "don't have any" : $"have {available}")}, are you sure you want to buy {(canBuy == 1 ? "one" : canBuy.ToString())} box{(canBuy == 1 ? "" : "es")} for {Context.Money(cost)} and open the {(toOpen == 1 ? "one" : toOpen.ToString())}?";

                var message = await Context.Channel.SendMessageAsync(text);
                ReactionMessageHelper.CreateConfirmReactionMessage(Context, message,
                    async onOkay =>
                    {
                        var modify = message.ModifyAsync(m => m.Content = $"{Context.WhatDoICall(Context.User)}, bought {(canBuy == 1 ? "one" : canBuy.ToString())} box{(canBuy == 1 ? "" : "es")} for {Context.Money(cost)}");
                        inventory.Currency -= cost;
                        inventory.RemoveBoxes(type, available);

                        await Open(inventory, variety, toOpen);
                        await modify;
                    },
                    async onDecline =>
                    {
                        await message.ModifyAsync(m => m.Content = $"Lootbox purchase cancelled");
                    });
                return;
            }

            [Command("view"), Summary("View your currently owned loot boxes")]
            public async Task View()
            {
                StringBuilder message = new StringBuilder();

                foreach (var (type, count) in Context.CallerProfile.LootBoxes)
                {
                    if (count > 0) message.Append($"{(count == 1 ? "one" : count.ToString())} {type} lootbox");
                }

                if (message.Length == 0)
                {
                    message.Append("no lootboxes");
                }

                await ReplyAsync($"{Context.User.Mention}, you have {message}");
            }

            private async Task Open(InventoryWrapper inventory, LootBox variety, int count)
            {
                StringBuilder message = new StringBuilder();

                for (int i = 0; i < count; i++)
                {
                    foreach (var (rarity, emoji) in variety.Open(Context.Bot, 0))
                    {
                        var trans = Transaction.FromLootbox(marketId: 0, buyer: inventory.UserId, variety.Name);

                        inventory.Add(new Models.Emoji
                        {
                            Owner = Context.User.Id,
                            Transactions = new List<TransactionInfo>() { Context.Bot.Clerk.Queue(trans).Receive() },
                            Unicode = emoji
                        }, true);

                        message.Append($"{rarity.LeftBracket}{emoji}{rarity.RightBracket}");
                    }
                    message.AppendLine();
                }

                var reply = ReplyAsync(message.ToString());
                inventory.Save();
                await reply;
            }
        }
    }
}
