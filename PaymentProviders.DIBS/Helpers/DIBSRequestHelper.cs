using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PaymentProviders.DIBS
{
    public class DIBSRequestHelper
    {
        private const string CaptureRequestUrl = "https://payment.architrade.com/cgi-bin/capture.cgi";
        private const string RefundRequestUrl = "https://payment.architrade.com/cgi-adm/refund.cgi";
        
        public DIBSConfiguration DIBSConfiguration { get; private set; }
        private readonly IOrderNumberGenerator _orderNumberGenerator;
        private readonly SiteContext _siteContext;

        public DIBSRequestHelper() : this(ServiceLocator.Current.GetInstance<IOrderNumberGenerator>(), ServiceLocator.Current.GetInstance<SiteContext>(), new DIBSConfiguration())
        {
        }

        public DIBSRequestHelper(IOrderNumberGenerator orderNumberGenerator, SiteContext siteContext, DIBSConfiguration dibsConfiguration)
        {
            _orderNumberGenerator = orderNumberGenerator;
            _siteContext = siteContext;
            DIBSConfiguration = dibsConfiguration;
        }

        public Dictionary<string, object> CreateRequestPaymentData(IPayment payment, ICart currentCart, string notifyUrl)
        {
            var currency = currentCart.Currency;
            var orderNumber = _orderNumberGenerator.GenerateOrderNumber(currentCart);
            var amount = Utilities.GetAmount(currency, payment.Amount);
            var mechantId = DIBSConfiguration.Merchant;

            var requestPaymentData = new Dictionary<string, object>();
            requestPaymentData.Add("paymentprovider", DIBSConfiguration.DIBSSystemName);
            requestPaymentData.Add("merchant", mechantId);
            requestPaymentData.Add("amount", amount);
            requestPaymentData.Add("currency", currency.CurrencyCode);
            requestPaymentData.Add("orderid", orderNumber);
            requestPaymentData.Add("uniqueoid", orderNumber);
            requestPaymentData.Add("accepturl", notifyUrl);
            requestPaymentData.Add("cancelurl", notifyUrl);
            requestPaymentData.Add("lang", DIBSLanguages.GetCurrentDIBSSupportedLanguage(_siteContext.LanguageName));
            requestPaymentData.Add("md5key", Utilities.GetMD5RequestKey(DIBSConfiguration, mechantId, orderNumber, currency, amount));
            requestPaymentData.Add("ordertext", string.Format("Payment for Order number {0}", orderNumber));
            requestPaymentData.Add("voucher", "yes");
            requestPaymentData.Add("decorator", "responsive");
            requestPaymentData.Add("test", "1"); // TODO: this parameter for development / payment testing.

            return requestPaymentData;
        }

        /// <summary>
        /// Posts the capture request to DIBS API.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        public string PostCaptureRequest(IPayment payment, IPurchaseOrder purchaseOrder)
        {
            return PostRequest(payment, purchaseOrder, CaptureRequestUrl);
        }

        /// <summary>
        /// Posts the refund request to DIBS API.
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="purchaseOrder"></param>
        public string PostRefundRequest(IPayment payment, IPurchaseOrder purchaseOrder)
        {
            return PostRequest(payment, purchaseOrder, RefundRequestUrl);
        }

        /// <summary>
        /// Posts the request to DIBS API.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        /// <param name="url">The URL.</param>
        /// <returns>A string contains result from DIBS API</returns>
        private string PostRequest(IPayment payment, IPurchaseOrder purchaseOrder, string url)
        {
            var orderId = purchaseOrder.OrderNumber;
            var currencyCode = purchaseOrder.Currency;
            var amount = Utilities.GetAmount(new Currency(currencyCode), payment.Amount);

            var request = new NameValueCollection();
            request.Add("merchant", DIBSConfiguration.Merchant);
            request.Add("transact", payment.TransactionID);
            request.Add("amount", amount);

            request.Add("currency", currencyCode);
            request.Add("orderId", orderId);
            request.Add("textreply", "yes");

            request.Add("md5key", Utilities.GetMD5RefundKey(DIBSConfiguration, DIBSConfiguration.Merchant, orderId, payment.TransactionID, amount));

            // in order to support split payment, make sure you have enabled Split payment for your account
            // more info go to: http://tech.dibspayment.com/flexwin_api_other_features_split_payment
            // to support split payment, comment out the next line and comment the line after that line.
            //request.Add("splitpay", "true"); // supports split payments
            request.Add("force", "yes");       // not support split payments

            var webClient = new WebClient
            {
                Credentials = new NetworkCredential(DIBSConfiguration.Merchant, DIBSConfiguration.Password)
            };
            var responseArray = webClient.UploadValues(url, "POST", request);
            return Encoding.ASCII.GetString(responseArray);
        }
    }
}
