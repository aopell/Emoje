using Discord;
using Discord.Commands;
using DiscordHackWeek2019.Models;
using DiscordHackWeek2019.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace DiscordHackWeek2019.Commands.Modules
{
    public class ProfileModule : ModuleBase<BotCommandContext>
    {
        [Command("join"), Alias("optin", "optout"), Summary("Sell your soul. For a modest amount of currency.")]
        public async Task Join()
        {
            if (Context.UserJoined(Context.User.Id))
            {
                // User has already joined
                await ReplyAsync("You've already joined. There's no going back."); // TODO: real message
                return;
            }

            Context.UserCollection.Insert(new User
            {
                UserId = Context.User.Id,
                Currency = Context.Bot.Options.StartingCurrency,
                CurrentInvestments = new PortfolioCollection(),
                Inventory = new Dictionary<string, List<Guid>>(),
                LootBoxes = new Dictionary<string, int>(),
                PreviousInvestments = new PortfolioCollection(),
                Transactions = new List<TransactionInfo>()
            });

            await ReplyAsync($"Welcome {Context.User.Mention}! {Context.Bot.Options.StartingCurrency} currency has been deposited to your account.");
        }

        [Command("profile"), Summary("Displays the profile of yourself or another user")]
        public async Task ViewProfile([Remainder] IUser user = null)
        {
            user = Context.GetUserOrSender(user);
            if (!Context.UserJoined(user.Id))
            {
                await ReplyAsync(Strings.UserJoinNeeded);
                return;
            }

            var profile = Context.GetProfile(user);

            var embed = Context.EmbedFromUser(user);
            embed.WithTitle("Profile");
            embed.AddField("Currency", profile.Currency, true);
            embed.AddField("Unique Emoji", profile.Inventory.Count, true);
            embed.AddField("Owned Loot Boxes", profile.LootBoxes.Count, true);
            embed.AddField("Transactions Completed", profile.Transactions.Count, true);
            embed.AddField("Unique Stocks", profile.CurrentInvestments.Stocks.Items.Count, true);
            embed.AddField("Unique Cryptocurrencies", profile.CurrentInvestments.Crypto.Items.Count, true);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("inventory"), Summary("Displays the inventory of yourself or another user")]
        public Task ViewInventory([Remainder] IUser user = null)
        {
            throw new NotImplementedException();
        }
    }
}
