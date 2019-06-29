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

        public const string SomethingChanged = "Something's different";

        // Emoji
        public const string moneyEmoji = "<:money:593606876051013678>";
        public const string emojiEmoji = "😃";
        public const string boxEmoji = "<:lootbox:593607880251277322>";
        public const string transactionEmoji = "<:transaction:593608952768626709>";
        public const string stockEmoji = "<:stocks:593606084460281867>";
        public const string cryptoEmoji = "<:crypto:593603698513412098>";

        // Rarities
        public const string commonLeft = "<:blue_l:593992724504641541>";
        public const string commonRight = "<:blue_r:593992909985021952>";
        public const string rareLeft = "<:green_l:593689834778918937>";
        public const string rareRight = "<:green_r:593690111435341827>";
        public const string epicLeft = "<:image7:593993152076054529>";
        public const string epicRight = "<:image7:593993222276251648>";
        public const string legendaryLeft = "<:legendary_l:593691613969121280>";
        public const string legendaryRight = "<:legendary_r:593691673289162774>";
    }
}
