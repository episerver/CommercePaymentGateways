using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PaymentProviders.PayPal.Tests.TestSupport
{
    class FakeOrderGroup : ICart
    {
        private Guid _customerId = Guid.NewGuid();
        private static int _counter;

        public OrderReference OrderLink { get; set; }

        public string Name { get; set; }

        public ICollection<IOrderForm> Forms { get; set; }

        public IMarket Market { get; set; }

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
            Market = new MarketImpl(MarketId.Default);
            Currency = new Currency(Currency.USD);
            OrderLink = new OrderReference(++_counter, "Default", _customerId, typeof(Cart));
            Properties = new Hashtable();
            Notes = new List<IOrderNote>();
        }
    }
}
