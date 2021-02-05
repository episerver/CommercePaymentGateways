using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Payments.Tokenization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using System;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx
{
    [ServiceConfiguration(typeof(IPaymentMethod))]
    public class AuthorizeTokenExPaymentMethod : IPaymentMethod
    {
        private readonly IOrderGroupFactory _orderGroupFactory;
        public Guid PaymentMethodId { get; }
        public string SystemKeyword => "AuthorizeTokenEx";
        public string Name { get; }
        public string Description { get; }

        public AuthorizeTokenExPaymentMethod(IOrderGroupFactory orderGroupFactory)
        {
            _orderGroupFactory = orderGroupFactory;
        }

        public IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var payment = orderGroup.CreatePayment(_orderGroupFactory, typeof(TokenizedPayment));
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();
            payment.PaymentMethodId = PaymentMethodId;
            payment.PaymentMethodName = SystemKeyword;
            return payment;
        }

        public bool ValidateData() => true;
    }
}
