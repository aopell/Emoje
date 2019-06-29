using Discord;
using Discord.Commands;
using DiscordHackWeek2019.Helpers;
using DiscordHackWeek2019.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("invest"), Alias("investments"), JoinRequired]
    public class InvestmentModule : ModuleBase<BotCommandContext>
    {
        [Command("in"), Alias("buy", "purchase", "order"), Summary("Invest in stock or cryptocurrency")]
        public async Task Buy([Summary("What kind of thing you want to buy, either stocks or crypto")] string type, [Summary("The name of the thing you want to buy")] string name, [Summary("How many you want to buy")] int amount = 1)
        {
            var symbolType = StockAPIHelper.GetSymbolTypeFromString(type);
            name = name.ToLower();
            if (amount <= 0) throw new DiscordCommandException($"{Context.User.Mention}, you can't purchase {(amount == 0 ? "" : "less than ")}no {(symbolType == SymbolType.Crypto ? name.ToUpper() : "shares")}");

            var profile = Context.CallerProfile;
            var info = await StockAPIHelper.GetSymbolInfo(name, symbolType);

            if (profile.Currency < info.LatestPrice) throw new DiscordCommandException($"{Context.User.Mention}, you need {Context.Money((int) info.LatestPrice - profile.Currency)} more to buy a single {(symbolType == SymbolType.Crypto ? name.ToUpper() : "share")}");

            var canBuy = (int)Math.Floor(profile.Currency / info.LatestPrice);
            int toBuy = Math.Min(canBuy, amount);

            string message;

            if (canBuy >= amount) message = $"{Context.User.Mention}, do you want to purchase {toBuy} {(symbolType == SymbolType.Crypto ? "" : $"{(toBuy == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money((int)(info.LatestPrice * toBuy))}? You currently have {Context.Money(profile.Currency)}.";
            else message = $"{Context.User.Mention}, you currently have {Context.Money(profile.Currency)}, that's only enough to buy {toBuy} {(symbolType == SymbolType.Crypto ? "" : $"{(toBuy == 1 ? "one share" : "shares")} in ")}{name.ToUpper()}. Do you still want to buy {(toBuy == 1 ? "it" : "them")} for {Context.Money((int)(info.LatestPrice * toBuy))}?";

            ReactionMessageHelper.CreateConfirmReactionMessage(Context, await ReplyAsync(message), onPurchase, onReject);

            async Task onPurchase(ReactionMessage m)
            {
                Context.ClearCachedValues();
                profile = Context.CallerProfile;
                if (profile.Currency < info.LatestPrice * toBuy)
                {
                    await m.Message.ModifyAsync(mod =>
                    {
                        mod.Content = "";
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithColor(Color.Red);
                        builder.WithTitle(Strings.SomethingChanged);
                        builder.WithDescription($"{Context.User.Mention}, you no longer have enough to buy {toBuy} {(symbolType == SymbolType.Crypto ? "" : $"{(toBuy == 1 ? "one share" : "shares")} in ")}{name.ToUpper()}");
                        mod.Embed = builder.Build();
                    });
                    return;
                }

                profile.Currency -= (int)(info.LatestPrice * toBuy);
                var portfolio = symbolType == SymbolType.Stock ? profile.CurrentInvestments.Stocks : profile.CurrentInvestments.Crypto;

                if (!portfolio.Items.ContainsKey(name))
                {
                    portfolio.Items.Add(name, new List<Investment>());
                }

                for (int i = 0; i < toBuy; i++)
                {
                    portfolio.Items[name].Add(new Investment { PurchasePrice = info.LatestPrice, PurchaseTimestamp = DateTimeOffset.Now });
                }

                Context.UserCollection.Update(profile);

                await m.Message.ModifyAsync(properties => properties.Content = $"{Context.WhatDoICall(Context.User)} bought {toBuy} {(symbolType == SymbolType.Crypto ? "" : $"{(toBuy == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money((int)(info.LatestPrice * toBuy))}");
            }

            async Task onReject(ReactionMessage m)
            {
                await m.Message.ModifyAsync(properties => properties.Content = "Purchase canceled");
            }
        }


        [Command("sell"), Summary("Sell one or more of your investments")]
        public async Task Sell([Summary("What kind of thing you want to sell, either stocks or crypto")] string type, [Summary("The name of the thing you want to sell")] string name, [Summary("How many you want to sell")] int amount = 1)
        {
            var symbolType = StockAPIHelper.GetSymbolTypeFromString(type);
            name = name.ToLower();
            if (amount <= 0) throw new DiscordCommandException($"{Context.User.Mention}, you can't sell {(amount == 0 ? "" : "less than ")}no {(symbolType == SymbolType.Crypto ? name.ToUpper() : "shares")}");

            var profile = Context.CallerProfile;
            var portfolio = symbolType == SymbolType.Stock ? profile.CurrentInvestments.Stocks : profile.CurrentInvestments.Crypto;
            var matching = portfolio.Items.GetValueOrDefault(name);

            if (matching == null) throw new DiscordCommandException($"{Context.User.Mention}, you don't have any investments in {name.ToUpper()}");

            SymbolInfo info = await StockAPIHelper.GetSymbolInfo(name, symbolType);

            int toSell = Math.Min(matching.Count(), amount);

            int totalSellAmount = toSell * (int) info.LatestPrice;

            string message;

            if (toSell >= amount) message = $"{Context.User.Mention}, do you want to sell {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money((int)(info.LatestPrice * toSell))}?";
            else message = $"{Context.User.Mention}, you currently have {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()}. Do you still want to sell {(toSell == 1 ? "it" : "them")} for {Context.Money((int)(info.LatestPrice * toSell))}?";

            ReactionMessageHelper.CreateConfirmReactionMessage(Context, await ReplyAsync(message), onSell, onReject);

            async Task onSell(ReactionMessage m)
            {
                Context.ClearCachedValues();
                profile = Context.CallerProfile;
                portfolio = symbolType == SymbolType.Stock ? profile.CurrentInvestments.Stocks : profile.CurrentInvestments.Crypto;
                matching = portfolio.Items.GetValueOrDefault(name);

                if (matching == null)
                {
                    await m.Message.ModifyAsync(mod =>
                    {
                        mod.Content = "";
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithColor(Color.Red);
                        builder.WithTitle(Strings.SomethingChanged);
                        builder.WithDescription($"{Context.User.Mention}, you no longer have any investments in {name.ToUpper()}");
                        mod.Embed = builder.Build();
                    });
                    return;
                }
                if (matching.Count < toSell)
                {
                    await m.Message.ModifyAsync(mod =>
                    {
                        mod.Content = "";
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithColor(Color.Red);
                        builder.WithTitle(Strings.SomethingChanged);
                        builder.WithDescription($"{Context.User.Mention}, you no longer have {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()}");
                        mod.Embed = builder.Build();
                    });
                    return;
                }

                var sells = matching.OrderByDescending(x => Math.Abs(info.LatestPrice - x.PurchasePrice)).Take(toSell).ToList();
                int totalPurchaseAmount = (int)sells.Sum(x => x.PurchasePrice);

                var previousPortfolio = symbolType == SymbolType.Stock ? profile.PreviousInvestments.Stocks : profile.PreviousInvestments.Crypto;
                if (!previousPortfolio.Items.ContainsKey(name))
                {
                    previousPortfolio.Items.Add(name, new List<Investment>());
                }
                var previousMatching = previousPortfolio.Items[name];

                profile.Currency += totalSellAmount;
                foreach (var inv in sells)
                {
                    matching.Remove(inv);
                    inv.SellPrice = info.LatestPrice;
                    inv.SellTimestamp = DateTimeOffset.Now;
                    previousMatching.Add(inv);
                }

                Context.UserCollection.Update(profile);

                int diff = totalSellAmount - totalPurchaseAmount;

                string modify;
                if (diff != 0) modify = $"{Context.WhatDoICall(Context.User)} sold {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money(totalSellAmount)}, {(diff > 0 ? "earning" : "losing")} {Math.Abs(diff)}. That's a {Math.Abs(diff / (double)totalPurchaseAmount):P2} {(diff > 0 ? "gain" : "loss")}";
                else modify = $"{Context.WhatDoICall(Context.User)} sold {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money(totalSellAmount)}, breaking even";

                await m.Message.ModifyAsync(p => p.Content = modify);
            }

            async Task onReject(ReactionMessage m)
            {
                await m.Message.ModifyAsync(p => p.Content = "Sale canceled");
            }
        }

        [Command, Summary("View your current investment portfolios")]
        public async Task View()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("**Stocks**");
            foreach (var kvp in Context.CallerProfile.CurrentInvestments.Stocks.Items)
            {
                if (kvp.Value.Count == 0) continue;
                b.AppendLine($"{kvp.Value.Count}x{kvp.Key.ToUpper()}");
            }
            b.AppendLine("**Crypto**");
            foreach (var kvp in Context.CallerProfile.CurrentInvestments.Crypto.Items)
            {
                if (kvp.Value.Count == 0) continue;
                b.AppendLine($"{kvp.Value.Count}x{kvp.Key.ToUpper()}");
            }
            await ReplyAsync(b.ToString());
        }
    }
}
