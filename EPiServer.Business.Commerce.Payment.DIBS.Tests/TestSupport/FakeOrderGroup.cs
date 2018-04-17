using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EPiServer.Business.Commerce.Payment.DIBS.Tests.TestSupport
{
    class FakeOrderGroup : ICart
    {
        private Guid _customerId = Guid.NewGuid();
        private static int _counter;

        public OrderReference OrderLink { get; set; }

        public string Name { get; set; }

        public ICollection<IOrderForm> Forms { get; set; }

        [Obsolete("This property is no longer used. Use IMarketService to get the market from MarketId instead. Will remain at least until May 2019.")]
        public IMarket Market { get; set; }

        public MarketId MarketId { get; set; }

        public string MarketName { get; set; }

        public bool PricesIncludeTax { get; set; }

        public ICollection<IOrderNote> Notes { get; }

        public Guid? Organization { get; set; }

        public OrderStatus OrderStatus { get; set; }

        public Currency Currency { get; set; }

        public Guid CustomerId { get; set; }

        public DateTime Created => DateTime.MaxValue;

        public DateTime? Modified => null;

        public Hashtable Properties { get; private set; }

        public FakeOrderGroup()
        {
            Forms = new List<IOrderForm>();
            var market = new MarketImpl(MarketId.Default);
            MarketId = market.MarketId;
            MarketName = market.MarketName;
            PricesIncludeTax = market.PricesIncludeTax;
            Currency = new Currency(Currency.USD);
            OrderLink = new OrderReference(++_counter, "Default", _customerId, typeof(Cart));
            Properties = new Hashtable();
            Notes = new List<IOrderNote>();
        }
    }
}
