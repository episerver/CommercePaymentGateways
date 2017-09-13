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
using System.Web;
using System.Web.Mvc;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    public class DIBSPaymentController : PageController<DIBSPage>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly DIBSRequestHelper _dibsRequestHelper;

        public DIBSPaymentController() : this (ServiceLocator.Current.GetInstance<IOrderRepository>())
        { }

        public DIBSPaymentController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
            _dibsRequestHelper = new DIBSRequestHelper();
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

            var payment = currentCart.Forms.SelectMany(f => f.Payments).FirstOrDefault(c => c.PaymentMethodId.Equals(_dibsRequestHelper.DIBSConfiguration.PaymentMethodId));
            if (payment == null)
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", Utilities.Translate("PaymentNotSpecified"));
            }

            InitializeReponse();

            var transactionRequest = new TransactionRequest(Request.Form, _dibsRequestHelper.DIBSConfiguration);
            if (transactionRequest.IsProcessable())
            {
                var cancelUrl = Utilities.GetUrlFromStartPageReferenceProperty("CheckoutPage"); // get link to Checkout page
                cancelUrl = UriSupport.AddQueryString(cancelUrl, "success", "false");
                cancelUrl = UriSupport.AddQueryString(cancelUrl, "paymentmethod", "dibs");
                var gateway = new DIBSPaymentGateway();

                var redirectUrl = cancelUrl;
                // Process successful transaction                        
                if (transactionRequest.IsSuccessful())
                {
                    var acceptUrl = Utilities.GetUrlFromStartPageReferenceProperty("DIBSPaymentLandingPage");
                    redirectUrl = gateway.ProcessSuccessfulTransaction(currentCart, payment, transactionRequest.Transact, transactionRequest.OrderId, acceptUrl, cancelUrl);
                }
                // Process unsuccessful transaction
                else if (transactionRequest.IsUnsuccessful())
                {
                    TempData["Message"] = Utilities.Translate("CancelMessage");
                    redirectUrl = gateway.ProcessUnsuccessfulTransaction(cancelUrl, Utilities.Translate("CancelMessage"));
                }

                return Redirect(redirectUrl);
            }

            var notifyUrl = UriSupport.AbsoluteUrlBySettings(Utilities.GetUrlFromStartPageReferenceProperty("DIBSPaymentPage"));

            var requestPaymentData = _dibsRequestHelper.CreateRequestPaymentData(payment, currentCart, notifyUrl);
            return new RedirectAndPostActionResult(_dibsRequestHelper.DIBSConfiguration.ProcessingUrl, requestPaymentData);
        }

        private void InitializeReponse()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            Response.Cache.SetNoStore();
            Response.AppendHeader("Pragma", "no-cache");
        }

    }
}