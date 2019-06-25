using EPiServer.Commerce.Order;
using System.Collections;
using System.Collections.Generic;
using Mediachase.Commerce.Orders;
using System;

namespace EPiServer.Business.Commerce.Payment.PayPal.Tests.TestSupport
{
    class FakeShipment : IShipment
    {
        private static int _counter;
        
        public int ShipmentId { get; set; }

        public ICollection<ILineItem> LineItems { get; set; }

        public OrderShipmentStatus OrderShipmentStatus { get; set; }

        public int? PickListId { get; set; }
        
        public string ShipmentTrackingNumber { get; set; }

        public IOrderAddress ShippingAddress { get; set; }

        public Guid ShippingMethodId { get; set; }

        public string ShippingMethodName { get; set; }

        public string WarehouseCode { get; set; }

        public Hashtable Properties { get; private set; }

        /// <inheritdoc />
        public IOrderGroup ParentOrderGroup => throw new NotImplementedException();

        public FakeShipment()
        {
            LineItems = new List<ILineItem>();
            ShipmentId = ++_counter;
            Properties = new Hashtable();
        }
    }
}
