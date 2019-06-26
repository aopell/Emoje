using Discord.Commands;
using DiscordHackWeek2019.Helpers;
using DiscordHackWeek2019.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("investments")]
    public class InvestmentModule : ModuleBase<BotCommandContext>
    {
        [Command("buy"), Alias("purchase", "order"), Summary("Invest in stock or cryptocurrency")]
        public async Task Buy(SymbolType type, string symbol, int amount = 1)
        {
            if (amount == 0) throw new ArgumentException("Amount must be positive and nonzero");
            var profile = Context.CallerProfile;
            var info = await StockAPIHelper.GetSymbolInfo(symbol, type);
            if (profile.Currency < info.LatestPrice * amount)
            {
                throw new InvalidOperationException("You do not have enough currency to make this purchase");
            }

            Context.CallerProfile.Currency -= (int)(info.LatestPrice * amount);
            var portfolio = type == SymbolType.Stock ? Context.CallerProfile.CurrentInvestments.Stocks : Context.CallerProfile.CurrentInvestments.Crypto;

            if (!portfolio.Items.ContainsKey(symbol))
            {
                portfolio.Items.Add(symbol, new List<Investment>());
            }

            for (int i = 0; i < amount; i++)
            {
                portfolio.Items[symbol].Add(new Investment { PurchasePrice = info.LatestPrice, PurchaseTimestamp = DateTimeOffset.Now });
            }

            Context.UserCollection.Update(Context.CallerProfile);

            await ReplyAsync($"Successfully purchased {amount}x{symbol} for {(int)(info.LatestPrice * amount)} currency. You now have {Context.CallerProfile.Currency} currency.");
        }


        [Command("sell"), Summary("Sell one of your investments")]
        public Task Sell()
        {
            throw new NotImplementedException();
        }

        [Command("view"), Summary("View your current investment portfolio")]
        public async Task View()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("Stocks");
            foreach (var kvp in Context.CallerProfile.CurrentInvestments.Stocks.Items)
            {
                b.AppendLine($"{kvp.Value.Count}x{kvp.Key}");
            }
            b.AppendLine("Crypto");
            foreach (var kvp in Context.CallerProfile.CurrentInvestments.Crypto.Items)
            {
                b.AppendLine($"{kvp.Value.Count}x{kvp.Key}");
            }
            await ReplyAsync(b.ToString());
        }
    }
}
