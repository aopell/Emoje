using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using DiscordHackWeek2019.Models;
using System.Linq;

namespace DiscordHackWeek2019.Helpers
{
    public static class MarketHelper
    {
        // Adds a listing to the market, transfering ownership of the emoji from the seller to the market
        // Returns the added listing
        public static Listing AddListing(BotCommandContext context, ulong marketId, Emoji emoji, int price)
        {
            // Get the corresponding market
            var marketsDB = context.Bot.DataProvider.GetCollection<Market>("markets");
            var market = marketsDB.GetById(marketId);
            if (market == null)
            {
                // Market does not exist, so make a new one
                market = new Market
                {
                    MarketId = marketId,
                    Transactions = new List<Transaction>(),
                    Listings = new Dictionary<string, List<Listing>>()
                };
                marketsDB.Insert(market);
            }

            // Add listing to the market
            if (!market.Listings.ContainsKey(emoji.Unicode))
            {
                // Make a new list of listings in the listing list if there is not a listing list to listen for listings
                market.Listings[emoji.Unicode] = new List<Listing>();
            }
            Listing toAdd = new Listing
            {
                UserId = context.User.Id,
                EmojiId = emoji.EmojiId,
                Price = price,
                Timestamp = DateTimeOffset.Now
            };
            market.Listings[emoji.Unicode].Add(toAdd);
            marketsDB.Update(market);

            return toAdd;
        }

        // Lets a user buy a listing, transfering ownership of the emoji from the market to the buyer
        public static void BuyListing(BotCommandContext context, ulong maretId, string emojiUnicode, ulong buyer)
        {
            // TODO: Take ownership of the cheapest listing and confirm purchase
            // TODO: Record transaction in market and emoji
            // TODO: Add to inventory
        }

        // Gets the emoji price. Returns float.NaN if there are none for sale
        public static (int, bool valid) GetEmojiPrice(BotCommandContext context, ulong marketId, string emojiUnicode)
        {
            // Make sure market exists
            var marketsDB = context.Bot.DataProvider.GetCollection<Market>("markets");
            var market = marketsDB.GetById(marketId);
            if (market == null)
            {
                // Market does not exist
                return (0, false);
            }

            if (market.Listings.ContainsKey(emojiUnicode))
            {
                Listing cheapest = CheapestListing(market.Listings[emojiUnicode]);
                if (cheapest != null)
                {
                    // There is a valid listing for sale
                    return (cheapest.Price, true);
                }
            }

            return (0, false);
        }

        private static Listing CheapestListing(List<Listing> listings)
        {
            return listings.OrderBy(listing => listing.Price).FirstOrDefault();
        }
    }
}
