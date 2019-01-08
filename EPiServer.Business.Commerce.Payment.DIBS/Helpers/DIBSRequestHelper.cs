using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    public class DIBSRequestHelper
    {
        private const string TransactionUrl = "https://api.dibspayment.com/merchant/v1/JSON/Transaction/";
        private readonly IOrderNumberGenerator _orderNumberGenerator;
        private readonly SiteContext _siteContext;

        public DIBSConfiguration DIBSConfiguration { get; }

        public DIBSRequestHelper() : this(ServiceLocator.Current.GetInstance<IOrderNumberGenerator>(), ServiceLocator.Current.GetInstance<SiteContext>(), new DIBSConfiguration())
        {
        }

        public DIBSRequestHelper(
            IOrderNumberGenerator orderNumberGenerator,
            SiteContext siteContext,
            DIBSConfiguration dibsConfiguration)
        {
            _orderNumberGenerator = orderNumberGenerator;
            _siteContext = siteContext;
            DIBSConfiguration = dibsConfiguration;
        }

        public Dictionary<string, object> CreateRequestPaymentData(IPayment payment, ICart currentCart, string notifyUrl)
        {
            var currencyCode = currentCart.Currency.CurrencyCode;

            // ref: https://tech.dibspayment.com/batch/d2integratedpwhostedinputparametersstandard
            var requestPaymentData = new Dictionary<string, object>
            {
                { "acceptReturnUrl", notifyUrl },
                { "amount", Utilities.GetAmount(currencyCode, payment.Amount) },
                { "billingAddress", payment.BillingAddress.Line1},
                { "billingAddress2", payment.BillingAddress.Line2},
                { "billingEmail", payment.BillingAddress.Email},
                { "billingFirstName", payment.BillingAddress.FirstName},
                { "billingLastName", payment.BillingAddress.LastName},
                { "billingMobile", payment.BillingAddress.DaytimePhoneNumber},
                { "billingPostalCode", payment.BillingAddress.PostalCode},
                { "billingPostalPlace", payment.BillingAddress.City},
                { "cancelReturnUrl", notifyUrl},
                { "currency", currencyCode },
                { "merchant", DIBSConfiguration.Merchant },
                { "orderId", _orderNumberGenerator.GenerateOrderNumber(currentCart) },
                { "test", 1 } // note: remove this param in production
            };

            requestPaymentData.Add("MAC", GetMACRequest(DIBSConfiguration, requestPaymentData));

            return requestPaymentData;
        }

        /// <summary>
        /// Posts the capture request to DIBS API.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        /// <returns>
        /// A dictionary containing:
        /// * declineReason: Reason for decline or failure, null for ACCEPT and PENDING
        /// * status: ACCEPT/DECLINE/ERROR/PENDING status of the call.
        /// PENDING means that the transaction has been successfully added for a batch capture.
        /// The result of the capture can be found in the administration.
        /// </returns>
        public Dictionary<string, string> PostCaptureRequest(IPayment payment, IPurchaseOrder purchaseOrder)
        {
            // ref: https://tech.dibspayment.com/batch/d2integratedpwapipaymentfunctionscapturetransaction

            var message = new Dictionary<string, object> {
                {"amount", Utilities.GetAmount(purchaseOrder.Currency, payment.Amount)},
                {"merchantId", DIBSConfiguration.Merchant},
                {"transactionId", payment.TransactionID}
            };

            message.Add("MAC", GetMACRequest(DIBSConfiguration, message));

            return PostToDIBS("CaptureTransaction", message);
        }

        /// <summary>
        /// Posts the refund request to DIBS API.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        /// <returns>
        /// A dictionary containing:
        /// * declineReason: Reason for decline or failure, null for ACCEPT and PENDING
        /// * status: ACCEPT/DECLINE/ERROR/PENDING status of the call.
        /// PENDING means that the transaction has been successfully added for a batch capture.
        /// The result of the capture can be found in the administration.
        /// </returns>
        public Dictionary<string, string> PostRefundRequest(IPayment payment, IPurchaseOrder purchaseOrder)
        {
            // ref: https://tech.dibspayment.com/batch/d2integratedpwapipaymentfunctionsrefundtransaction

            var message = new Dictionary<string, object> {
                {"amount", Utilities.GetAmount(purchaseOrder.Currency, payment.Amount)},
                {"merchantId", DIBSConfiguration.Merchant},
                {"transactionId", payment.TransactionID}
            };

            message.Add("MAC", GetMACRequest(DIBSConfiguration, message));

            return PostToDIBS("RefundTransaction", message);
        }

        protected virtual string GetMACRequest(DIBSConfiguration configuration, Dictionary<string, object> message) => Utilities.GetMACRequest(configuration, message);

        private Dictionary<string, string> PostToDIBS(string paymentFunction, Dictionary<string, object> data)
        {
            var postUrl = TransactionUrl;
            switch (paymentFunction)
            {
                case "AuthorizeCard":
                    postUrl += "AuthorizeCard";
                    break;
                case "AuthorizeTicket":
                    postUrl += "AuthorizeTicket";
                    break;
                case "CancelTransaction":
                    postUrl += "CancelTransaction";
                    break;
                case "CaptureTransaction":
                    postUrl += "CaptureTransaction";
                    break;
                case "CreateTicket":
                    postUrl += "CreateTicket";
                    break;
                case "RefundTransaction":
                    postUrl += "RefundTransaction";
                    break;
                case "Ping":
                    postUrl += "Ping";
                    break;
                default:
                    postUrl = null;
                    break;
            }

            var jsonData = "request=" + JsonConvert.SerializeObject(data);
            var encodedData = Encoding.ASCII.GetBytes(jsonData);

            //Using HttpWebRequest for posting and receiving response
            var request = (HttpWebRequest)WebRequest.Create(postUrl);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = encodedData.Length;
            request.Timeout = 15000;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(encodedData, 0, encodedData.Length);
            }

            //Receive response
            var responseDict = new Dictionary<string, string> { };
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            }
            catch (WebException ex)
            {
                throw ex;
            }

            return responseDict;
        }
    }
}
