using EPiServer.Commerce.Order;
using System;

namespace EPiServer.Business.Commerce.Payment.PayPal.Tests.TestSupport
{
    public class FakeOrderGroupBuilderFactory : IOrderGroupBuilder
    {
        public Type[] ForType
        {
            get { return new Type[] { typeof(FakeOrderGroup), typeof(FakePurchaseOrder) }; }
        }

        public int SortOrder
        {
            get
            {
                return 0;
            }
        }

        public IOrderForm CreateOrderForm()
        {
            return new FakeOrderForm();
        }

        public IShipment CreateShipment()
        {
            return new FakeShipment();
        }

        public ILineItem CreateLineItem(string code)
        {
            return new FakeLineItem() { Code = code };
        }

        public IOrderAddress CreateOrderAddress()
        {
            throw new NotImplementedException();
        }

        public IOrderNote CreateOrderNote()
        {
            return new FakeOrderNote();
        }

        public IPayment CreatePayment()
        {
            return new FakePayment();
        }
        
        public IPayment CreatePayment(Type paymentType)
        {
            throw new NotImplementedException();
        }

        public ICreditCardPayment CreateCardPayment()
        {
            throw new NotImplementedException();
        }

        public ITaxValue CreateTaxValue()
        {
            throw new NotImplementedException();
        }
    }
}
