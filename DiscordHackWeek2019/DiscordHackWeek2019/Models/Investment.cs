﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordHackWeek2019.Models
{
    public class Investment
    {
        public float PurchasePrice { get; set; }
        public DateTimeOffset PurchaseTimestamp { get; set; }
        public float? SellPrice { get; set; }
        public DateTimeOffset? SellTimestamp { get; set; }
    }

    public class InvestmentPortfolio
    {
        public Dictionary<string, List<Investment>> Items { get; set; }

        public InvestmentPortfolio()
        {
            Items = new Dictionary<string, List<Investment>>();
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
