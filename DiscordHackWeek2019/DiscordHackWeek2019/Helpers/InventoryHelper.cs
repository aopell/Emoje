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

        public InventoryWrapper(BotCommandContext ctx, User user)
        {
            Context = ctx;
            UserId = user.UserId;
            this.user = user;
        }

        private bool dirty = false;
        private User user;
        public User User
        {
            get
            {
                if (user == null)
                {
                    user = Context.GetProfile(UserId);
                }
                return user;
            }
        }

        public int Currency
        {
            get => User.Currency;
            set => User.Currency = value;
        }

        public IEnumerable<Emoji> Enumerate() => User.Inventory
                                                    .SelectMany(x => x.Value)
                                                    .Select(id => Context.EmojiCollection.FindById(id));

        public IEnumerable<Emoji> Enumerate(string emoji) => User.Inventory[emoji].Select(id => Context.EmojiCollection.FindById(id));

        public void Add(Emoji emoji, bool brandNew = false)
        {
            emoji.Owner = UserId;
            Guid id;
            if (brandNew)
            {
                id = Context.EmojiCollection.Insert(emoji);
            }
            else
            {
                id = emoji.EmojiId;
                Context.EmojiCollection.Update(emoji);
            }

            List<Guid> list;
            if (!User.Inventory.ContainsKey(emoji.Unicode))
            {
                list = new List<Guid>();
                User.Inventory.Add(emoji.Unicode, list);
            }
            else
            {
                list = User.Inventory[emoji.Unicode];
            }

            list.Add(id);
            dirty = true;
        }

        public void AddLootbox(string variety, int count = 1) => AddLootbox(variety, (uint) count);
        public void AddLootbox(string variety, uint count = 1)
        {
            if (User.LootBoxes.ContainsKey(variety))
            {
                User.LootBoxes[variety] += count;
            }
            else
            {
                User.LootBoxes.Add(variety, count);
            }

            dirty = true;
        }

        public void Save()
        {
            if (dirty && user != null)
            {
                Context.UserCollection.Update(user);
            }
        }
    }
}
