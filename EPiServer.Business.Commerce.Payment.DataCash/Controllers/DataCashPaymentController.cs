using EPiServer;
using EPiServer.Commerce.Order;
using EPiServer.Editor;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Security;
using System;
using System.Linq;
using System.Web.Mvc;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    public class DataCashPaymentController : PageController<DataCashPage>
    {
        private readonly IOrderRepository _orderRepository;

        public DataCashPaymentController() : this (ServiceLocator.Current.GetInstance<IOrderRepository>())
        { }

        public DataCashPaymentController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public ActionResult Index()
        {
            if (PageEditing.PageIsInEditMode)
            {
                return new EmptyResult();
            }

            var currentCart = _orderRepository.LoadCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(), Cart.DefaultName);

            if (!currentCart.Forms.Any() || !currentCart.GetFirstForm().Payments.Any())
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", Utilities.Translate("GenericError"));
            }

            // Get DataCash payment by PaymentMethodName, instead of get the first payment in the list.
            var dataCashPaymentMethod = DataCashConfiguration.GetDataCashPaymentMethod().PaymentMethod.Rows[0] as Mediachase.Commerce.Orders.Dto.PaymentMethodDto.PaymentMethodRow;
            var paymentMethodId = dataCashPaymentMethod != null ? dataCashPaymentMethod.PaymentMethodId : Guid.Empty;
            
            var payment = currentCart.GetFirstForm().Payments.FirstOrDefault(c => c.PaymentMethodId.Equals(paymentMethodId));
            if (payment == null)
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", Utilities.Translate("PaymentNotSpecified"));
            }

            string merchantRef = payment.Properties[DataCashPaymentGateway.DataCashMerchantReferencePropertyName] as string;
            if (string.IsNullOrEmpty(merchantRef))
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", Utilities.Translate("GenericError"));
            }

            // Redirect customer to receipt page
            var acceptUrl = Utilities.GetUrlFromStartPageReferenceProperty("DataCashPaymentLandingPage");
            var cancelUrl = Utilities.GetUrlFromStartPageReferenceProperty("CheckoutPage"); // get link to Checkout page
            cancelUrl = UriSupport.AddQueryString(cancelUrl, "success", "false");
            cancelUrl = UriSupport.AddQueryString(cancelUrl, "paymentmethod", "DataCash");

            var gateway = new DataCashPaymentGateway();
            string redirectUrl;
            if (string.Equals(Request.QueryString["accept"], "true") && Utilities.GetMD5Key(merchantRef + "accepted") == Request.QueryString["hash"])
            {
                redirectUrl = gateway.ProcessSuccessfulTransaction(currentCart, payment, acceptUrl, cancelUrl);
            }
            else
            {
                redirectUrl = gateway.ProcessUnsuccessfulTransaction(cancelUrl, Utilities.Translate("GenericError"));
            }

            return Redirect(redirectUrl);
        }
    }
}