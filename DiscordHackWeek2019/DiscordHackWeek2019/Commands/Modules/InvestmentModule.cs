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
        public async Task Buy([Summary("What kind of thing you want to buy, either stocks or crypto")] string type, [Summary("The name of the thing you want to buy")] string name, [Summary("How many you want to buy")] long amount = 1)
        {
            var symbolType = StockAPIHelper.GetSymbolTypeFromString(type);
            name = name.ToLower();
            if (amount <= 0) throw new DiscordCommandException("Number too low", $"{Context.User.Mention}, you can't purchase {(amount == 0 ? "" : "less than ")}no {(symbolType == SymbolType.Crypto ? name.ToUpper() : "shares")}");

            var profile = Context.CallerProfile;
            var info = await StockAPIHelper.GetSymbolInfo(name, symbolType);

            if (profile.Currency < info.LatestPrice) throw new DiscordCommandException("Not enough currency", $"{Context.User.Mention}, you need {Context.Money((long) info.LatestPrice - profile.Currency)} more to buy a single {(symbolType == SymbolType.Crypto ? name.ToUpper() : "share")}");

            long price = ((long)Math.Ceiling(info.LatestPrice));
            long canBuy = profile.Currency / price;
            long toBuy = Math.Min(canBuy, amount);

            string message;

            if (canBuy >= amount) message = $"{Context.User.Mention}, do you want to purchase {toBuy} {(symbolType == SymbolType.Crypto ? "" : $"{(toBuy == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money(price * toBuy)}? You currently have {Context.Money(profile.Currency)}.";
            else message = $"{Context.User.Mention}, you currently have {Context.Money(profile.Currency)}, that's only enough to buy {toBuy} {(symbolType == SymbolType.Crypto ? "" : $"{(toBuy == 1 ? "one share" : "shares")} in ")}{name.ToUpper()}. Do you still want to buy {(toBuy == 1 ? "it" : "them")} for {Context.Money(price * toBuy)}?";

            ReactionMessageHelper.CreateConfirmReactionMessage(Context, await ReplyAsync(message), onPurchase, onReject);

            async Task onPurchase(ReactionMessage m)
            {
                Context.ClearCachedValues();
                profile = Context.CallerProfile;
                if (profile.Currency < price * toBuy)
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

                profile.Currency -= price * toBuy;
                var investments = symbolType == SymbolType.Stock ? profile.Investments.Stocks.Active : profile.Investments.Crypto.Active;

                if (!investments.ContainsKey(name))
                {
                    investments.Add(name, new List<Investment>());
                }

                investments[name].Add(new Investment
                {
                    Amount = toBuy,
                    PurchasePrice = info.LatestPrice,
                    PurchaseTimestamp = DateTimeOffset.Now
                });

                Context.UserCollection.Update(profile);

                await m.Message.ModifyAsync(properties => properties.Content = $"{Context.WhatDoICall(Context.User)} bought {toBuy} {(symbolType == SymbolType.Crypto ? "" : $"{(toBuy == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money(price * toBuy)}");
            }

            async Task onReject(ReactionMessage m)
            {
                await m.Message.ModifyAsync(properties => properties.Content = $"Purchase of {(symbolType == SymbolType.Crypto ? "" : $"{ (toBuy == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} canceled");
            }
        }


        [Command("sell"), Summary("Sell one or more of your investments")]
        public async Task Sell([Summary("What kind of thing you want to sell, either stocks or crypto")] string type, [Summary("The name of the thing you want to sell")] string name, [Summary("How many you want to sell")] long amount = 1)
        {
            var symbolType = StockAPIHelper.GetSymbolTypeFromString(type);
            name = name.ToLower();
            if (amount <= 0) throw new DiscordCommandException("Number too low", $"{Context.User.Mention}, you can't sell {(amount == 0 ? "" : "less than ")}no {(symbolType == SymbolType.Crypto ? name.ToUpper() : "shares")}");

            var profile = Context.CallerProfile;
            var investments = symbolType == SymbolType.Stock ? profile.Investments.Stocks.Active : profile.Investments.Crypto.Active;
            var investmentsInName = investments.GetValueOrDefault(name);

            if (investmentsInName == null || investmentsInName.Count == 0) throw new DiscordCommandException("Nothing to sell", $"{Context.User.Mention}, you don't have any investments in {name.ToUpper()}");

            SymbolInfo info = await StockAPIHelper.GetSymbolInfo(name, symbolType);

            long toSell = Math.Min(investmentsInName.Sum(x => x.Amount), amount);

            long price = (long)Math.Ceiling(info.LatestPrice);
            long totalSellAmount = toSell * price;

            string message;

            if (toSell >= amount) message = $"{Context.User.Mention}, do you want to sell {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money(price * toSell)}?";
            else message = $"{Context.User.Mention}, you currently have {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()}. Do you still want to sell {(toSell == 1 ? "it" : "them")} for {Context.Money(price * toSell)}?";

            ReactionMessageHelper.CreateConfirmReactionMessage(Context, await ReplyAsync(message), onSell, onReject);

            async Task onSell(ReactionMessage m)
            {
                Context.ClearCachedValues();
                profile = Context.CallerProfile;
                investments = symbolType == SymbolType.Stock ? profile.Investments.Stocks.Active : profile.Investments.Crypto.Active;
                investmentsInName = investments.GetValueOrDefault(name);

                if (investmentsInName == null)
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
                if (investmentsInName.Sum(x => x.Amount) < toSell)
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

                var sold = new List<Investment>();
                Investment remainderToReturn = null;
                Investment remainderToStore = null;

                long totalPurchaseAmount = 0;

                long stillToSell = toSell;
                foreach (var inv in investmentsInName.OrderByDescending(x => Math.Abs(price - x.PurchasePrice)))
                {
                    if (stillToSell >= inv.Amount)
                    {
                        totalPurchaseAmount += inv.Amount * (long)Math.Ceiling(inv.PurchasePrice);
                        stillToSell -= inv.Amount;
                        sold.Add(inv);
                    }
                    else
                    {
                        totalPurchaseAmount += stillToSell * (long)Math.Ceiling(inv.PurchasePrice);

                        remainderToStore = new Investment
                        {
                            Amount = stillToSell,
                            PurchasePrice = inv.PurchasePrice,
                            PurchaseTimestamp = inv.PurchaseTimestamp,
                        };

                        remainderToReturn = new Investment
                        {
                            Amount = inv.Amount - stillToSell,
                            PurchasePrice = inv.PurchasePrice,
                            PurchaseTimestamp = inv.PurchaseTimestamp,
                        };

                        stillToSell = 0;

                        break;
                    }
                    if (stillToSell == 0) break;
                }

                if (stillToSell > 0)
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

                var oldInvestments = (symbolType == SymbolType.Stock) ? profile.Investments.Stocks.Old : profile.Investments.Crypto.Old;

                List<Investment> oldInvestmentsInName;
                if (!oldInvestments.ContainsKey(name))
                {
                    oldInvestmentsInName = new List<Investment>();
                    oldInvestments.Add(name, oldInvestmentsInName);
                }
                else oldInvestmentsInName = oldInvestments[name];

                profile.Currency += totalSellAmount;
                foreach (var inv in sold)
                {
                    investmentsInName.Remove(inv);
                    inv.SellPrice = info.LatestPrice;
                    inv.SellTimestamp = DateTimeOffset.Now;
                    oldInvestmentsInName.Add(inv);
                }

                if (remainderToReturn != null) investmentsInName.Add(remainderToReturn);
                
                if (remainderToStore != null)
                {
                    remainderToStore.SellPrice = info.LatestPrice;
                    remainderToStore.SellTimestamp = DateTimeOffset.Now;
                    oldInvestmentsInName.Add(remainderToStore);
                }

                Context.UserCollection.Update(profile);

                long diff = totalSellAmount - totalPurchaseAmount;

                string modify;
                if (diff != 0) modify = $"{Context.WhatDoICall(Context.User)} sold {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money(totalSellAmount)}, {(diff > 0 ? "earning" : "losing")} {Math.Abs(diff)}. That's a {Math.Abs(diff / (double)totalPurchaseAmount):P2} {(diff > 0 ? "gain" : "loss")}";
                else modify = $"{Context.WhatDoICall(Context.User)} sold {toSell} {(symbolType == SymbolType.Crypto ? "" : $"{(toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money(totalSellAmount)}, breaking even";

                await m.Message.ModifyAsync(p => p.Content = modify);
            }

            async Task onReject(ReactionMessage m)
            {
                await m.Message.ModifyAsync(p => p.Content = $"Sale of {(symbolType == SymbolType.Crypto ? "" : $"{ (toSell == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} canceled");
            }
        }

        [Command, Summary("View your current investment portfolio")]
        public async Task View([Summary("Whether you want to view your current or old investments, should be something like \"active\", or \"old\"")] string activeOrNot = "active", [Summary("What kind of investment you want to see, either \"stocks\", \"crypto\", or \"all\"")] string type = "all")
        {
            var profile = Context.CallerProfile.Investments;

            bool active = "active".StartsWith(activeOrNot);
            var crypto = (active ? profile.Crypto.Active : profile.Crypto.Old).Select(s => (SymbolType.Crypto, s));
            var stocks = (active ? profile.Stocks.Active : profile.Stocks.Old).Select(s => (SymbolType.Stock, s));

            IEnumerable<(SymbolType t, KeyValuePair<string, List<Investment>> kv)> pre;
            if ("all".StartsWith(type))
            {
                pre = crypto.Concat(stocks);
            }
            else
            {
                SymbolType st = StockAPIHelper.GetSymbolTypeFromString(type);

                pre = st == SymbolType.Crypto ? crypto : stocks;
            }

            IEnumerable<(SymbolType t, string s, Investment i)> investments = pre.SelectMany(p => p.kv.Value.Select(i => (p.t, p.kv.Key, i)));

            if (investments.Count() == 0) throw new DiscordCommandException("Nothing to show", $"{Context.User.Mention}, you have no {(active ? "current" : "old")} investments {("all".StartsWith(type) ? "at all" : "of that type")}");

            string title = $"{(active ? "Current investments" : "Investment history")}";

            const int NUM_PER_PAGE = 10;

            int totalPages = (investments.Count() + NUM_PER_PAGE - 1) / NUM_PER_PAGE;

            Embed getPage(int page)
            {
                EmbedBuilder builder = Context.EmbedFromUser(Context.User);
                StringBuilder stringBuilder = new StringBuilder();
                List<string> contents = new List<string>();
                foreach (var (t, name, investment) in investments.Skip((page - 1) * NUM_PER_PAGE).Take(NUM_PER_PAGE))
                {
                    stringBuilder.Append($"{(t == SymbolType.Crypto ? Strings.cryptoEmoji : Strings.stockEmoji)} {investment.Amount} {(t == SymbolType.Crypto ? "" : $"{(investment.Amount == 1 ? "one share" : "shares")} in ")}{name.ToUpper()} for {Context.Money(investment.Amount * (long)Math.Ceiling(investment.PurchasePrice))}");
                    if (investment.SellPrice != null) stringBuilder.Append($", sold for {Context.Money(investment.Amount * (long)Math.Ceiling(investment.SellPrice ?? 0))}");
                    stringBuilder.AppendLine();
                }

                builder.AddField(new EmbedFieldBuilder().WithName(title).WithValue(stringBuilder.ToString()));
                builder.WithFooter(new EmbedFooterBuilder().WithText($"Page {page} of {totalPages}"));

                return builder.Build();
            }

            var message = await ReplyAsync(embed: getPage(1));

            ReactionMessageHelper.CreatePaginatedMessage(Context, message, totalPages, 1, pg => Task.FromResult(("", getPage(pg.CurrentPage))));
        }
    }
}
