using DiscordHackWeek2019.Commands;
using DiscordHackWeek2019.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LiteDB;
using System.Drawing;

namespace DiscordHackWeek2019.Helpers
{
    public class InventoryWrapper
    {
        public ulong UserId { get; private set; }

        private BotCommandContext Context;

        public InventoryWrapper(BotCommandContext ctx, ulong userId)
        {
            Context = ctx;
            UserId = userId;
        }

        private bool dirty = false;
        private User _user = null;
        public User User
        {
            get
            {
                if (_user == null)
                {
                    _user = Context.GetProfile(UserId);
                }
                return _user;
            }
        }

        public IEnumerable<Emoji> Enumerate() => User.Inventory
                                                    .SelectMany(x => x.Value)
                                                    .Select(id => Context.EmojiCollection.FindById(id));

        public IEnumerable<Emoji> Enumerate(string emoji) => User.Inventory[emoji].Select(id => Context.EmojiCollection.FindById(id));

        public void Add(Emoji emoji)
        {
            emoji.Owner = UserId;
            Guid id;
            if (Context.EmojiCollection.FindById(emoji.EmojiId) == null)
            {
                id = Context.EmojiCollection.Insert(emoji);
            }
            else
            {
                id = emoji.EmojiId;
                Context.EmojiCollection.Update(emoji);
            }

            User.Inventory.GetValueOrDefault(emoji.Unicode, new List<Guid>()).Add(id);
            dirty = true;
        }

        public void Save()
        {
            if (dirty && _user != null)
            {
                Context.UserCollection.Update(UserId, _user);
            }
        }
    }
}
