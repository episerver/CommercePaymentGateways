using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using System.Collections;
using System.Collections.Generic;

namespace EPiServer.Business.Commerce.Payment.PayPal.Tests.TestSupport
{
    class FakeOrderForm : IOrderForm
    {
        private readonly int _orderFormId;
        private static int _counter;
        private readonly IList<PromotionInformation> _promotions = new List<PromotionInformation>();
        private readonly ICollection<string> _couponCodes = new List<string>();
        
        public int OrderFormId
        {
            get { return _orderFormId; }
        }

        public FakeOrderGroup Parent { get; set; }

        public decimal AuthorizedPaymentTotal { get; set; }

        public decimal CapturedPaymentTotal { get; set; }

        public decimal HandlingTotal { get; set; }

        public string Name { get; set; }

        public bool PricesIncludeTax => Parent?.PricesIncludeTax ?? false;

        public ICollection<IShipment> Shipments { get; set; }

        public IList<PromotionInformation> Promotions
        {
            get { return _promotions; }
        }

        public ICollection<string> CouponCodes
        {
            get { return _couponCodes; }
        }

        public ICollection<IPayment> Payments { get; set; }

        public Hashtable Properties { get; private set; }
        
        public FakeOrderForm()
        {
            Shipments = new List<IShipment>();
            _orderFormId = ++_counter;
            Properties = new Hashtable();
            Payments = new List<IPayment>();
        }

        public static FakeOrderForm CreateOrderForm(Hashtable properties = null)
        {
            return new FakeOrderForm
            {
                AuthorizedPaymentTotal = 0,
                CapturedPaymentTotal = 0,
                HandlingTotal = 10,
                Properties = properties ?? new Hashtable()
            };
        }
    }
}