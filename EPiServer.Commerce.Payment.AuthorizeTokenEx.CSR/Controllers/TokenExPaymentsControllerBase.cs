using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Payments.Tokenization;
using EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR.Models;
using EPiServer.Commerce.UI.CustomerService.Controllers;
using EPiServer.Commerce.UI.CustomerService.Extensions;
using EPiServer.Commerce.UI.CustomerService.Models;
using EPiServer.Commerce.UI.CustomerService.Services;
using EPiServer.ServiceLocation;
using System.Linq;
using System.Web.Http;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR.Controllers
{
    public abstract class TokenExPaymentsControllerBase<TOrderGroup> : CustomerServiceApiController where TOrderGroup : class, IOrderGroup
    {
        private readonly PaymentHandler<TOrderGroup> _paymentHandler;

        public TokenExPaymentsControllerBase()
          : this(ServiceLocator.Current.GetInstance<PaymentHandler<TOrderGroup>>()) { }

        public TokenExPaymentsControllerBase(PaymentHandler<TOrderGroup> paymentHandler)
        {
            _paymentHandler = paymentHandler;
        }

        public virtual IHttpActionResult AddPayment(int orderGroupId, int formId, [FromBody] TokenizationPaymentModel model)
        {
            if (!_paymentHandler.ValidatePaymentMethod<PaymentModel>(orderGroupId, formId, model, out var results))
            {
                return BadRequest(string.Join(",", results.Select(x => x.Message)));
            }

            var payment = _paymentHandler.AddPaymentToOrder<PaymentModel>(orderGroupId, formId, model,
                  (paymentModel, paymentMethod, orderGroup) => AddPaymentToOrderGroup(paymentModel, paymentMethod, orderGroup), out results);

            return results.Count > 0
                ? BadRequest(string.Join(",", results.Select(x => x.Message)))
                : (IHttpActionResult)Ok(new PaymentModel(payment, model.Amount.Currency));
        }

        private IPayment AddPaymentToOrderGroup(PaymentModel model, IPaymentMethod paymentMethod, IOrderGroup orderGroup)
        {
            var payment = model.CreatePayment(paymentMethod, orderGroup);

            if (payment is ITokenizedPayment tokenPayment &&
                model is TokenizationPaymentModel tokenPaymentModel)
            {
                tokenPayment.Token = tokenPaymentModel.Token;
                tokenPayment.ExpirationMonth = tokenPaymentModel.ExpirationMonth;
                tokenPayment.ExpirationYear = tokenPaymentModel.ExpirationYear;
            }

            return payment;
        }
    }
}