using EPiServer.Business.Commerce.Payment.PayPal;
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

namespace EPiServer.Business.Commerce.Payment.PayPal
{
    public class PayPalPaymentController : PageController<PayPalPage>
    {
        private readonly IOrderRepository _orderRepository;

        public PayPalPaymentController() : this (ServiceLocator.Current.GetInstance<IOrderRepository>())
        { }

        public PayPalPaymentController(IOrderRepository orderRepository)
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

            var paymentConfiguration = new PayPalConfiguration();
            var payment = currentCart.Forms.SelectMany(f => f.Payments).FirstOrDefault(c => c.PaymentMethodId.Equals(paymentConfiguration.PaymentMethodId));
            if (payment == null)
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", Utilities.Translate("PaymentNotSpecified"));
            }

            var orderNumber = payment.Properties[PayPalPaymentGateway.PayPalOrderNumberPropertyName] as string;
            if (string.IsNullOrEmpty(orderNumber))
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", Utilities.Translate("PaymentNotSpecified"));
            }

            // Redirect customer to receipt page
            var cancelUrl = Utilities.GetUrlFromStartPageReferenceProperty("CheckoutPage"); // get link to Checkout page
            cancelUrl = UriSupport.AddQueryString(cancelUrl, "success", "false");
            cancelUrl = UriSupport.AddQueryString(cancelUrl, "paymentmethod", "paypal");

            var gateway = new PayPalPaymentGateway();
            var redirectUrl = cancelUrl;
            if (string.Equals(Request.QueryString["accept"], "true") && Utilities.GetAcceptUrlHashValue(orderNumber) == Request.QueryString["hash"])
            {
                var acceptUrl = Utilities.GetUrlFromStartPageReferenceProperty("PayPalPaymentLandingPage");
                redirectUrl = gateway.ProcessSuccessfulTransaction(currentCart, payment, acceptUrl, cancelUrl);
            }
            else if (string.Equals(Request.QueryString["accept"], "false") && Utilities.GetCancelUrlHashValue(orderNumber) == Request.QueryString["hash"])
            {
                TempData["Message"] = Utilities.Translate("CancelMessage");
                redirectUrl = gateway.ProcessUnsuccessfulTransaction(cancelUrl, Utilities.Translate("CancelMessage"));
            }

            return Redirect(redirectUrl);
        }
    }
}