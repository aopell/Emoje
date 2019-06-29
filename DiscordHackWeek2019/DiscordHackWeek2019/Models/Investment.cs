using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Investment
    {
        public long Amount { get; set; }
        public float PurchasePrice { get; set; }
        public DateTimeOffset PurchaseTimestamp { get; set; }
        public float? SellPrice { get; set; }
        public DateTimeOffset? SellTimestamp { get; set; }
    }

    public class InvestmentPortfolio
    {
        public Dictionary<string, List<Investment>> Active { get; set; }
        public Dictionary<string, List<Investment>> Old { get; set; }

        public InvestmentPortfolio()
        {
            Active = new Dictionary<string, List<Investment>>();
            Old = new Dictionary<string, List<Investment>>();
        }
    }

    public class PortfolioCollection
    {
        public InvestmentPortfolio Stocks { get; set; }
        public InvestmentPortfolio Crypto { get; set; }

        public PortfolioCollection()
        {
            Stocks = new InvestmentPortfolio();
            Crypto = new InvestmentPortfolio();
        }
    }
}
