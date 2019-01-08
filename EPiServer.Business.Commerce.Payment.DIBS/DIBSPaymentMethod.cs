using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using System;
using System.Linq;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    [ServiceConfiguration(typeof(IPaymentMethod))]
    public class DIBSPaymentMethod : IPaymentMethod
    {
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly PaymentMethodDto.PaymentMethodRow _paymentMethod;

        public Guid PaymentMethodId { get; }
        public string SystemKeyword { get; }
        public string Name { get; }
        public string Description { get; }

        public DIBSPaymentMethod() : this(ServiceLocator.Current.GetInstance<IOrderGroupFactory>())
        {
        }

        public DIBSPaymentMethod(IOrderGroupFactory orderGroupFactory)
        {
            _orderGroupFactory = orderGroupFactory;
            _paymentMethod = DIBSConfiguration.GetDIBSPaymentMethod()?.PaymentMethod?.FirstOrDefault();

            if (_paymentMethod == null)
            {
                return;
            }

            PaymentMethodId = _paymentMethod.PaymentMethodId;
            SystemKeyword = _paymentMethod.SystemKeyword;
            Name = _paymentMethod.Name;
            Description = _paymentMethod.Description;
        }

        public IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var type = Type.GetType(_paymentMethod.PaymentImplementationClassName);
            var payment = type == null ? orderGroup.CreatePayment(_orderGroupFactory) : orderGroup.CreatePayment(_orderGroupFactory, type);

            payment.PaymentMethodId = _paymentMethod.PaymentMethodId;
            payment.PaymentMethodName = _paymentMethod.SystemKeyword;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();

            payment.TransactionType = TransactionType.Authorization.ToString();

            return payment;
        }
        
        public bool ValidateData()
        {
            return true;
        }
    }
}