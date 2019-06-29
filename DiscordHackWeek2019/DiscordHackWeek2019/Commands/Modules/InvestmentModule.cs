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
        public async Task Buy(SymbolType type, string symbol, int amount = 1)
        {
            symbol = symbol.ToLower();
            if (amount <= 0) throw new DiscordCommandException("Amount must be positive and nonzero");

            var profile = Context.CallerProfile;
            var info = await StockAPIHelper.GetSymbolInfo(symbol, type);

            if (profile.Currency < info.LatestPrice * amount) throw new DiscordCommandException("You do not have enough currency to make this purchase");

            ReactionMessageHelper.CreateConfirmReactionMessage(Context, await ReplyAsync($"Purchase {amount} x {symbol.ToUpper()} for {(int)(info.LatestPrice * amount)} currency. You currently have {profile.Currency} currency."), onPurchase, onReject, false, 30000);

            async Task onPurchase(ReactionMessage m)
            {
                Context.ClearCachedValues();
                profile = Context.CallerProfile;
                if (profile.Currency < info.LatestPrice * amount) throw new DiscordCommandException("You do not have enough currency to make this purchase");

                profile.Currency -= (int)(info.LatestPrice * amount);
                var portfolio = type == SymbolType.Stock ? profile.CurrentInvestments.Stocks : profile.CurrentInvestments.Crypto;

                if (!portfolio.Items.ContainsKey(symbol))
                {
                    portfolio.Items.Add(symbol, new List<Investment>());
                }

                for (int i = 0; i < amount; i++)
                {
                    portfolio.Items[symbol].Add(new Investment { PurchasePrice = info.LatestPrice, PurchaseTimestamp = DateTimeOffset.Now });
                }

                Context.UserCollection.Update(profile);

                await m.Message.ModifyAsync(properties => properties.Content = $"Successfully purchased {amount} x {symbol.ToUpper()} for {(int)(info.LatestPrice * amount)} currency. You now have {profile.Currency} currency.");
            }

            async Task onReject(ReactionMessage m)
            {
                await m.Message.ModifyAsync(properties => properties.Content = "Purchase canceled");
            }
        }


        [Command("sell"), Summary("Sell one or more of your investments")]
        public async Task Sell(SymbolType type, string symbol, int amount = 1)
        {
            symbol = symbol.ToLower();
            if (amount <= 0) throw new DiscordCommandException("Amount must be positive and nonzero");

            var profile = Context.CallerProfile;
            var portfolio = type == SymbolType.Stock ? profile.CurrentInvestments.Stocks : profile.CurrentInvestments.Crypto;
            var matching = portfolio.Items.GetValueOrDefault(symbol);

            if (matching == null) throw new DiscordCommandException("You don't have any investments with that symbol");
            if (matching.Count < amount) throw new DiscordCommandException($"You only have {matching.Count} investments but attempted to sell {amount}.");

            var info = await StockAPIHelper.GetSymbolInfo(symbol, type);

            int totalSellAmount = (int)(amount * info.LatestPrice);

            ReactionMessageHelper.CreateConfirmReactionMessage(Context, await ReplyAsync($"Sell {amount} x {symbol.ToUpper()} for {totalSellAmount} currency?"), onSell, onReject, false, 30000);

            async Task onSell(ReactionMessage m)
            {
                Context.ClearCachedValues();
                profile = Context.CallerProfile;
                portfolio = type == SymbolType.Stock ? profile.CurrentInvestments.Stocks : profile.CurrentInvestments.Crypto;
                matching = portfolio.Items.GetValueOrDefault(symbol);

                if (matching == null) throw new DiscordCommandException("You don't have any investments with that symbol");
                if (matching.Count < amount) throw new DiscordCommandException($"You only have {matching.Count} investments but attempted to sell {amount}.");

                var toSell = matching.OrderByDescending(x => Math.Abs(info.LatestPrice - x.PurchasePrice)).Take(amount).ToList();
                int totalPurchaseAmount = (int)toSell.Sum(x => x.PurchasePrice);

                var previousPortfolio = type == SymbolType.Stock ? profile.PreviousInvestments.Stocks : profile.PreviousInvestments.Crypto;
                if (!previousPortfolio.Items.ContainsKey(symbol))
                {
                    previousPortfolio.Items.Add(symbol, new List<Investment>());
                }
                var previousMatching = previousPortfolio.Items[symbol];

                profile.Currency += totalSellAmount;
                foreach (var inv in toSell)
                {
                    matching.Remove(inv);
                    inv.SellPrice = info.LatestPrice;
                    inv.SellTimestamp = DateTimeOffset.Now;
                    previousMatching.Add(inv);
                }

                Context.UserCollection.Update(profile);

                if (totalSellAmount > totalPurchaseAmount)
                {
                    await m.Message.ModifyAsync(p => p.Content = $"You sold {amount} x {symbol.ToUpper()} for {totalSellAmount} currency. You made a profit of {totalSellAmount - totalPurchaseAmount} currency! That's a {(totalSellAmount - totalPurchaseAmount) / (double)totalPurchaseAmount:P2} gain! You now have {profile.Currency} currency.");
                }
                else if (totalSellAmount < totalPurchaseAmount)
                {
                    await m.Message.ModifyAsync(p => p.Content = $"You sold {amount} x {symbol.ToUpper()} for {totalSellAmount} currency. You lost {Math.Abs(totalSellAmount - totalPurchaseAmount)} currency ({(totalSellAmount - totalPurchaseAmount) / (double)totalPurchaseAmount:P2}). You now have {profile.Currency} currency.");
                }
                else
                {
                    await m.Message.ModifyAsync(p => p.Content = $"You sold {amount} x {symbol.ToUpper()} for {totalSellAmount} currency. You broke even! You now have {profile.Currency} currency.");
                }
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
