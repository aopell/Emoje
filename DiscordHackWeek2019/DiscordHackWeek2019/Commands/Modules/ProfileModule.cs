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
                Inventory = new Dictionary<string, List<ulong>>(),
                LootBoxes = new Dictionary<string, int>(),
                PreviousInvestments = new PortfolioCollection(),
                Transactions = new List<TransactionInfo>()
            });


            await ReplyAsync($"Welcome {Context.User.Mention}! You have currency."); // TODO: real message
        }

        [Command("profile"), Summary("Displays the profile of yourself or another user")]
        public async Task ViewProfile([Remainder] IUser user = null)
        {
            if (!Context.UserJoined(Context.GetUserOrSender(user).Id))
            {
                await ReplyAsync(Strings.UserJoinNeeded);
                return;
            }

            if (user == null)
            {
                await ReplyAsync($"You have {Context.CallerProfile.Currency} money");
            }
            else
            {
                await ReplyAsync($"{Context.WhatDoICall(user)} has {Context.UserCollection.GetById(user.Id).Currency} money");
            }

        }

        [Command("inventory"), Summary("Displays the inventory of yourself or another user")]
        public Task ViewInventory([Remainder] IUser user = null)
        {
            throw new NotImplementedException();
        }
    }
}
