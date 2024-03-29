﻿using DiscordHackWeek2019.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Helpers
{
    public static class StockAPIHelper
    {
        private static string Token => DiscordBot.MainInstance.Secret.IexCloudSecret;

        private static string GenerateQuoteUrl(string symbol) => $"https://cloud.iexapis.com/stable/stock/{symbol}/quote?token={Token}";

        private static ObjectCache StockCache = new MemoryCache("stocks");
        private static readonly CacheItemPolicy Policy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(5) };

        private static string[] STOCKS = { "stocks" };
        private static string[] CRYPTO = { "crypto" };

        public static async Task<SymbolInfo> GetSymbolInfo(string symbol, SymbolType type = SymbolType.Stock)
        {
            string origSymbol = symbol;
            if (type == SymbolType.Crypto)
            {
                symbol += "USDT";
            }

            if (StockCache.Contains(symbol))
            {
                return StockCache.Get(symbol) as SymbolInfo;
            }

            HttpClient client = new HttpClient();
            var result = await client.GetAsync(GenerateQuoteUrl(symbol));

            if (!result.IsSuccessStatusCode)
            {
                if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new DiscordCommandException("Symbol does not exist", $"The symbol `{origSymbol}` does not correspond to an existing security");
                }
                throw new InvalidOperationException($"{result.StatusCode} {result.StatusCode.ToString()}: {result.ReasonPhrase}");
            }

            string json = await result.Content.ReadAsStringAsync();

            var info = JsonConvert.DeserializeObject<SymbolInfo>(json);

            StockCache.Add(symbol, info, Policy);

            return info;
        }

        public static SymbolType GetSymbolTypeFromString(string type)
        {
            foreach (var s in STOCKS)
            {
                if (s.StartsWith(type)) return SymbolType.Stock;
            }

            foreach (var s in CRYPTO)
            {
                if (s.StartsWith(type)) return SymbolType.Crypto;
            }

            throw new DiscordCommandException("Bad argument", $"\"{type}\" should either be \"stocks\" or \"crypto\"");
        }
    }

    public class SymbolInfo
    {
        public string Symbol { get; set; }
        public string CompanyName { get; set; }
        public float LatestPrice { get; set; }
    }

    public enum SymbolType
    {
        Stock,
        Crypto,
    }
}
