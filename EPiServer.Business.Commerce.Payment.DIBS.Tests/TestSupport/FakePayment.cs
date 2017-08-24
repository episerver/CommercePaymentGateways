using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;
using System;
using System.Collections;

namespace EPiServer.Business.Commerce.Payment.DIBS.Tests.TestSupport
{
    class FakePayment : IPayment
    {
        private static int _counter;

        public FakePayment()
        {
            Properties = new Hashtable();
            PaymentId = ++_counter;
        }

        public Hashtable Properties { get; private set; }
        public decimal Amount { get; set; }
        public string AuthorizationCode { get; set; }
        public IOrderAddress BillingAddress { get; set; }
        public string CustomerName { get; set; }
        public string ImplementationClass { get; set; }
        public int PaymentId { get; set; }
        public Guid PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; }
        public PaymentType PaymentType { get; set; }
        public string ProviderTransactionID { get; set; }
        public string Status { get; set; }
        public string TransactionID { get; set; }
        public string TransactionType { get; set; }
        public string ValidationCode { get; set; }

        
        public static FakePayment CreatePayment(decimal amount, 
            PaymentType paymentType, 
            Guid paymentMethodId, 
            string authorizationCode = "",
            string customerName = "",
            string implementationClass = "",
            string paymentMethodName = "",
            string providerTransactionID = "",
            string status = "",
            string transactionID = "",
            string transactionType = "",
            string validationCode = "",
            IOrderAddress billingAddress = null, 
            Hashtable properties = null)
        {
            return new FakePayment
            {
                Amount = amount,
                AuthorizationCode = authorizationCode,
                BillingAddress = billingAddress ?? FakeOrderAddress.CreateOrderAddress(),
                CustomerName = customerName,
                ImplementationClass = implementationClass,
                PaymentMethodId = paymentMethodId,
                PaymentMethodName = paymentMethodName,
                PaymentType = paymentType,
                ProviderTransactionID = providerTransactionID,
                Status = status,
                TransactionID = transactionID,
                TransactionType = transactionType,
                ValidationCode = validationCode,
                Properties = properties ?? new Hashtable()
            };
        }
    }
}
