using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Commands
{
    public static class Strings
    {
        // Messages
        public static string UserJoinNeeded(Discord.IUser u) { return $"{u.Mention}, try joining with `+join` first!"; }
        public const string UserDisabled = "Sorry, but you do not have permission to use the bot";

        // Emoji
        public const string moneyEmoji = "<:money:593606876051013678>";
        public const string emojiEmoji = "😃";
        public const string boxEmoji = "<:lootbox:593607880251277322>";
        public const string transactionEmoji = "<:transaction:593608952768626709>";
        public const string stockEmoji = "<:stocks:593606084460281867>";
        public const string cryptoEmoji = "<:crypto:593603698513412098>";

        // Rarities
        public const string commonLeft = "<:common_l:593993338261078026>";
        public const string commonRight = "<:common_r:593993401414975499>";
        public const string rareLeft = "<:rare_l:593689863275020288>";
        public const string rareRight = "<:rare_r:593690111951241236>";
        public const string epicLeft = "<:epic_l:593689846921429026>";
        public const string epicRight = "<:epic_r:593690111628279809>";
        public const string legendaryLeft = "<:legendary_l:593691613969121280>";
        public const string legendaryRight = "<:legendary_r:593691673289162774>";
    }
}
