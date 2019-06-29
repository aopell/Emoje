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
        [Group("lootbox"), Alias("box", "lootboxes", "loot", "l"), JoinRequired]
        public class LootBoxModule : ModuleBase<BotCommandContext>
        {
            [Command("buy"), Alias("purchase"), Summary("Buy one or more lootboxes")]
            public async Task Buy([Summary("How many boxes to buy")] int count = 1, [Summary("The type of box to buy")] string type = "normal")
            {
                var availableVarieties = LootBoxHelper.GetAllLootBoxNames(Context.Guild.Id);
                if (!availableVarieties.Contains(type)) throw new DiscordCommandException("Bad lootbox type", $"{type} isn't a lootbox you can buy, try {(availableVarieties.Count == 1 ? "\"" : "one of these:\n")}{string.Join(", ", availableVarieties)}{(availableVarieties.Count == 1 ? "\"" : "")}");

                var variety = LootBoxHelper.GetAllLootBoxes(Context)[type];

                var inventory = Context.GetInventory(Context.User);

                if (inventory.Currency < variety.Cost) throw new DiscordCommandException("Not enough currency", $"{Context.WhatDoICall(Context.User)}, you can't afford to buy one");

                int actualCount = (int) Math.Min(inventory.Currency / variety.Cost, count);
                long cost = actualCount * variety.Cost;

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
            public async Task Open([Summary("How many boxes to open")] int count = 1, [Summary("The type of box to open")] string type = "normal")
            {
                var availableVarieties = LootBoxHelper.GetAllLootBoxNames(Context.Guild.Id);
                if (!availableVarieties.Contains(type)) throw new DiscordCommandException("Bad lootbox type", $"{type} isn't a lootbox you can buy, try {(availableVarieties.Count == 1 ? "\"" : "one of these:\n")}{string.Join(", ", availableVarieties)}{(availableVarieties.Count == 1 ? "\"" : "")}");

                if (count > 5)
                {
                    count = 5;
                    await ReplyAsync($"{Context.User.Mention}, you can only open 5 boxes at a time");
                }

                var inventory = Context.GetInventory(Context.User);
                var variety = LootBoxHelper.GetAllLootBoxes(Context)[type];

                int available = inventory.GetNumLootBoxes(type);

                if (available >= count)
                {
                    inventory.RemoveBoxes(type, count);
                    await Open(inventory, variety, count);
                    return;
                }

                if (available == 0 && inventory.Currency < variety.Cost) throw new DiscordCommandException("Not enough currency", $"{Context.User.Mention}, you don't have any to open and can't afford to buy one");

                int needToBuy = count - available;

                int canBuy = (int)Math.Min(inventory.Currency / variety.Cost, needToBuy);

                if (canBuy == 0)
                {
                    inventory.RemoveBoxes(type, available);
                    await ReplyAsync($"{Context.User.Mention}, you only had {available} to open");
                    await Open(inventory, variety, available);
                    return;
                }

                long cost = canBuy * variety.Cost;

                int toOpen = available + canBuy;

                string text;

                if (needToBuy > canBuy) text = $"{Context.User.Mention}, you {(available == 0 ? "don't have any" : $"have {(available == 1 ? "one" : available.ToString())}")} right now, and you can only afford {(canBuy == 1 ? "one" : canBuy.ToString())} box{(canBuy == 1 ? "" : "es")}, do you still want to purchase {(canBuy == 1 ? "it" : "them")} for {Context.Money(cost)} and open {(toOpen == 1 ? "one" : toOpen.ToString())}?";
                else text = $"{Context.User.Mention}, you {(available == 0 ? "don't have any" : $"have {(available == 1 ? "one" : available.ToString())}")}, are you sure you want to buy {(canBuy == 1 ? "one" : canBuy.ToString())} box{(canBuy == 1 ? "" : "es")} for {Context.Money(cost)} and open the {(toOpen == 1 ? "one" : toOpen.ToString())}?";

                var message = await Context.Channel.SendMessageAsync(text);
                ReactionMessageHelper.CreateConfirmReactionMessage(Context, message,
                    async onOkay =>
                    {
                        Context.ClearCachedValues();

                        inventory = Context.GetInventory(Context.User);

                        if (inventory.GetNumLootBoxes(type) < available)
                        {
                            await message.ModifyAsync(mod =>
                            {
                                mod.Content = "";
                                EmbedBuilder builder = new EmbedBuilder();
                                builder.WithColor(Color.Red);
                                builder.WithTitle(Strings.SomethingChanged);
                                builder.WithDescription($"{Context.WhatDoICall(Context.User)}, you can no longer have the {(available == 1 ? "one" : available.ToString())} box{(available == 1 ? "" : "es")} to open");
                                mod.Embed = builder.Build();
                            });
                            return;
                        }

                        if (inventory.Currency < cost)
                        {
                            await message.ModifyAsync(mod =>
                            {
                                mod.Content = "";
                                EmbedBuilder builder = new EmbedBuilder();
                                builder.WithColor(Color.Red);
                                builder.WithTitle(Strings.SomethingChanged);
                                builder.WithDescription($"{Context.WhatDoICall(Context.User)}, you can no longer afford to buy the {(canBuy == 1 ? "one" : canBuy.ToString())}");
                                mod.Embed = builder.Build();
                            });
                            return;
                        }

                        var modify = message.ModifyAsync(m => m.Content = $"{Context.WhatDoICall(Context.User)}, bought {(canBuy == 1 ? "one" : canBuy.ToString())} box{(canBuy == 1 ? "" : "es")} for {Context.Money(cost)}");
                        inventory.Currency -= cost;
                        if (available > 0) inventory.RemoveBoxes(type, available);

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
                    if (count > 0) message.Append($"{(count == 1 ? "one" : count.ToString())} {type} lootbox{(count == 1 ? "" : "es")}");
                }

                if (message.Length == 0)
                {
                    message.Append("no lootboxes");
                }

                await ReplyAsync($"{Context.User.Mention}, you have {message}");
            }

            private async Task Open(InventoryWrapper inventory, LootBox variety, int count)
            {
                StringBuilder text = new StringBuilder();
                IUserMessage message = null;
                if (count > 1)
                {
                    string m = string.Concat(Enumerable.Repeat(variety.Emote.ToString() + "\n", count));
                    message = await ReplyAsync(m);
                    await Task.Delay(1000);
                }

                for (int i = 0; i < count; i++)
                {
                    var box = variety.Open(Context.Bot, 0);
                    if (count == 1)
                    {
                        message = await ReplyAsync(variety.Emote.ToString());
                        await Task.Delay(1000);
                        StringBuilder animation = new StringBuilder();

                        foreach (var (rarity, emoji) in box)
                        {
                            animation.Append($"{rarity.LeftBracket}❔{rarity.RightBracket}");
                        }
                        await message.ModifyAsync(m => m.Content = animation.ToString());
                        await Task.Delay(1000);
                        animation.Clear();
                    }

                    foreach (var (rarity, emoji) in box)
                    {
                        var trans = Transaction.FromLootbox(marketId: 0, buyer: inventory.UserId, variety.Name);

                        inventory.Add(new Models.Emoji
                        {
                            Owner = Context.User.Id,
                            Transactions = new List<TransactionInfo>() { Context.Bot.Clerk.Queue(trans).Receive() },
                            Unicode = emoji
                        }, true);

                        text.Append($"{rarity.LeftBracket}{emoji}{rarity.RightBracket}");
                    }
                    text.AppendLine();
                }

                inventory.Save();
                await message.ModifyAsync(m => m.Content = text.ToString());
            }
        }
    }
}
