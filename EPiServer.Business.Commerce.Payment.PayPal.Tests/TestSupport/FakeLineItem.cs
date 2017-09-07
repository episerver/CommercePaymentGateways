﻿using EPiServer.Commerce.Order;
using Mediachase.Commerce.Inventory;
using System.Collections;

namespace EPiServer.Business.Commerce.Payment.PayPal.Tests.TestSupport
{
    class FakeLineItem : ILineItem
    {
        private static int _counter;

        public int LineItemId { get; set; }

        public string Code { get; set; }

        public string DisplayName { get; set; }

        public decimal PlacedPrice { get; set; }

        public decimal Quantity { get; set; }

        public decimal ReturnQuantity { get; set; }

        public InventoryTrackingStatus InventoryTrackingStatus { get; set; }

        public bool IsInventoryAllocated { get; set; }

        public bool IsGift { get; set; }

        public Hashtable Properties { get; private set; }

        public FakeLineItem()
        {
            LineItemId = ++_counter;
            Properties = new Hashtable();
        }

    }
}