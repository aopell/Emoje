using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Commands.Modules
{
    [Group("investments")]
    public class InvestmentModule : ModuleBase<SocketCommandContext>
    {
        [Command("buy"), Alias("purchase", "order"), Summary("Invest in stock or cryptocurrency")]
        public Task Buy()
        {
            throw new NotImplementedException();
        }

        [Command("sell"), Summary("Sell one of your investments")]
        public Task Sell()
        {
            throw new NotImplementedException();
        }

        [Command("view"), Summary("View your current investment portfolio")]
        public Task View()
        {
            throw new NotImplementedException();
        }
    }
}
