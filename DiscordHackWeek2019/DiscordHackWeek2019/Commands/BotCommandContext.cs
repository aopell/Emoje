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

        private LiteCollection<User> _userCollection;

        private LiteCollection<Models.Emoji> _emojiCollection;

        private User _user = null;
        private User CurrentUser => _user ?? (_user = UserCollection.GetById(User.Id));

        public LiteCollection<User> UserCollection
        {
            get
            {
                if (_userCollection == null)
                {
                    _userCollection = Bot.DataProvider.GetCollection<User>("users");
                }

                return _userCollection;
            }
        }

        public LiteCollection<Models.Emoji> EmojiCollection
        {
            get
            {
                if (_emojiCollection == null)
                {
                    _emojiCollection = Bot.DataProvider.GetCollection<Models.Emoji>("emoji");
                }

                return _emojiCollection;
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

        public bool UserEnabled(ulong id)
        {
            var u = UserCollection.GetById(id);
            return !u.Disabled;
        }

        public string WhatDoICall(ulong userId) => WhatDoICall(Guild.GetUser(userId));

        public string WhatDoICall(IUser user)
        {
            return user is IGuildUser gu && !string.IsNullOrEmpty(gu.Nickname) ? gu.Nickname : user.Username;
        }

        public string GetMarketName(ulong marketId)
        {
            if (marketId == 0) return "the global market";

            var guild = Bot.Client.GetGuild(marketId);

            if (guild == null) return "some unknown market";

            return $"the {guild.Name} market";
        }

        public EmbedBuilder EmbedFromUser(IUser user) => new EmbedBuilder().WithAuthor(WhatDoICall(user), user.AvatarUrlOrDefaultAvatar());

        public User GetProfile(IUser user) => GetProfile(user.Id);
        public User GetProfile(ulong user) => UserCollection.GetById(user) ?? throw new KeyNotFoundException("User did not exist. Check first, dummy");

        public string WhoDoYouCall() => "Ghostbusters";

        public IUser GetUserOrSender(IUser user = null) => user ?? User;

        public InventoryWrapper GetInventory(IUser user) => GetInventory(user.Id);
        public InventoryWrapper GetInventory(ulong user) => new InventoryWrapper(this, user);
        public InventoryWrapper GetInventory(User user) => new InventoryWrapper(this, user);

        public string Money(long amount) => $"e̩̍{amount}";

        public void ClearCachedValues()
        {
            _user = null;
            _userCollection = null;
            _emojiCollection = null;
        }

        private LiteCollection<Market> marketCollection;

        public BotCommandContext(DiscordSocketClient client, SocketUserMessage msg, DiscordBot bot) : base(client, msg)
        {
            Bot = bot;
        }
    }
}
