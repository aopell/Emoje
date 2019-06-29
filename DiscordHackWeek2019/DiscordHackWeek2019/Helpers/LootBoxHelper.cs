using Discord;
using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordHackWeek2019.Helpers
{
    public static class LootBoxHelper
    {
        public static Dictionary<string, LootBox> LootBoxVarieties { get; set; } = new Dictionary<string, LootBox>
        {
            ["normal"] = new LootBox { Cost = 500, Name = "normal", Emote = Emote.Parse("<:lootbox:593607880251277322>"), RarityRatios = new[] { new[] { 55, 25, 15, 5 }, new[] { 55, 25, 15, 5 }, new[] { 55, 25, 15, 5 }, new[] { 55, 25, 15, 5 } } }
        };

        public static IReadOnlyCollection<string> GetAllLootBoxNames(BotCommandContext ctx) => GetAllLootBoxNames(ctx.Guild.Id);
        public static IReadOnlyCollection<string> GetAllLootBoxNames(ulong localMarket) => LootBoxVarieties.Keys; // TODO: add custom varieties
        public static Dictionary<string, LootBox> GetAllLootBoxes(BotCommandContext ctx) => GetAllLootBoxes(ctx.Guild.Id); // TODO: add custom varieties
        public static Dictionary<string, LootBox> GetAllLootBoxes(ulong localMarket) => LootBoxVarieties; // TODO: add custom varieties
    }

    public class LootBox
    {
        public string Name { get; set; }
        public long Cost { get; set; }
        public int Items => RarityRatios.Length;
        public int[][] RarityRatios { get; set; }
        public Emote Emote { get; set; }
        public List<(Rarity rarity, string emoji)> Open(DiscordBot bot, ulong marketId)
        {
            List<(Rarity, string)> results = new List<(Rarity, string)>();

            var emojisByRarity = MarketHelper.GetRarities(bot, marketId);

            for (int i = 0; i < Items; i++)
            {
                int[] ratios = RarityRatios[i];
                int sum = ratios.Sum();
                int random = bot.Random.Next(1, sum + 1);
                int seqsum = 0;
                for (int j = 0; j < ratios.Length; j++)
                {
                    seqsum += ratios[j];
                    if (random <= seqsum)
                    {
                        var rarity = Rarity.Rarities[j];
                        var valid = emojisByRarity[rarity];
                        results.Add((rarity, valid[bot.Random.Next(valid.Count)]));
                        break;
                    }
                }
            }

            return results;
        }
    }
}
