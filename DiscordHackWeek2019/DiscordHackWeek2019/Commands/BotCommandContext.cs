using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordHackWeek2019.Helpers;
using DiscordHackWeek2019.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Commands
{
    public class BotCommandContext : SocketCommandContext
    {
        public DiscordBot Bot { get; set; }

        private User _user = null;
        private User CurrentUser => _user ?? (_user = UserCollection.GetById(User.Id));

        public LiteCollection<User> UserCollection
        {
            get
            {
                if (userCollection == null)
                {
                    userCollection = Bot.DataProvider.GetCollection<User>("users");
                }

                return userCollection;
            }
        }

        public LiteCollection<Models.Emoji> EmojiCollection
        {
            get
            {
                if (emojiCollection == null)
                {
                    emojiCollection = Bot.DataProvider.GetCollection<Models.Emoji>("emoji");
                }

                return emojiCollection;
            }
        }

        public LiteCollection<Market> MarketCollection
        {
            get
            {
                if (marketCollection == null)
                {
                    marketCollection = Bot.DataProvider.GetCollection<Market>("markets");
                }

                return marketCollection;
            }
        }

        public User CallerProfile => CurrentUser ?? throw new KeyNotFoundException("User did not exist. Check first, dummy");

        public bool UserJoined(ulong id)
        {
            var u = UserCollection.GetById(id);
            return u != null;
        }

        public string WhatDoICall(IUser user)
        {
            var gu = user as IGuildUser;
            return gu != null && !string.IsNullOrEmpty(gu.Nickname) ? gu.Nickname : user.Username;
        }

        public EmbedBuilder EmbedFromUser(IUser user) => new EmbedBuilder().WithAuthor(WhatDoICall(user), user.AvatarUrlOrDefaultAvatar());

        public User GetProfile(IUser user) => GetProfile(user.Id);
        public User GetProfile(ulong user) => UserCollection.GetById(user) ?? throw new KeyNotFoundException("User did not exist. Check first, dummy");

        public string WhoDoYouCall() => "Ghostbusters";

        public IUser GetUserOrSender(IUser user = null) => user ?? User;

        public InventoryWrapper GetInventory(IUser user) => GetInventory(user.Id);
        public InventoryWrapper GetInventory(ulong user) => new InventoryWrapper(this, user);
        public InventoryWrapper GetInventory(User user) => new InventoryWrapper(this, user);

        private LiteCollection<User> userCollection;

        private LiteCollection<Models.Emoji> emojiCollection;

        private LiteCollection<Market> marketCollection;

        public BotCommandContext(DiscordSocketClient client, SocketUserMessage msg, DiscordBot bot) : base(client, msg)
        {
            Bot = bot;
        }
    }
}
