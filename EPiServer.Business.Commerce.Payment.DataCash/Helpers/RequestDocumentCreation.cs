using DataCash;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    public class RequestDocumentCreation
    {
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly ILineItemCalculator _lineItemCalculator;
        private readonly DataCashConfiguration _dataCashConfiguration;

        public RequestDocumentCreation() : this (ServiceLocator.Current.GetInstance<IOrderGroupCalculator>(), ServiceLocator.Current.GetInstance<ILineItemCalculator>(), new DataCashConfiguration())
        {
        }

        public RequestDocumentCreation(IOrderGroupCalculator orderGroupCalculator, ILineItemCalculator lineItemCalculator, DataCashConfiguration dataCashConfiguration)
        {
            _orderGroupCalculator = orderGroupCalculator;
            _lineItemCalculator = lineItemCalculator;
            
            _dataCashConfiguration = dataCashConfiguration;
        }

        public Document CreateDocumentForPaymentCheckout(ICart cart, IPayment payment, string notifyUrl)
        {
            var merchantReference = payment.Properties[DataCashPaymentGateway.DataCashMerchantReferencePropertyName] as string;

            var requestDoc = CreateDocument();
            requestDoc.set("Request.Transaction.TxnDetails.merchantreference", merchantReference);
            requestDoc.setWithAttributes("Request.Transaction.TxnDetails.amount", cart.GetTotal(_orderGroupCalculator).ToString("0.##"), new Hashtable() { { "currency", cart.Currency.ToString() } });
            requestDoc.set("Request.Transaction.HpsTxn.method", "setup");
            requestDoc.set("Request.Transaction.HpsTxn.page_set_id", _dataCashConfiguration.PaymentPageId);
            // set the return Url to our landing page, to automatic click on "Review & Place order" button, instead of force user to click on it
            requestDoc.set("Request.Transaction.HpsTxn.return_url", notifyUrl);

            return requestDoc;
        }

        public Document CreateDocumentForPreAuthenticateRequest(IPayment payment, IOrderForm orderForm, Currency orderGroupCurrency)
        {
            var dataCashReference = payment.Properties[DataCashPaymentGateway.DataCashReferencePropertyName] as string;
            var merchantReference = payment.Properties[DataCashPaymentGateway.DataCashMerchantReferencePropertyName] as string;

            var requestDoc = CreateDocument();
            requestDoc.set("Request.Transaction.TxnDetails.merchantreference", merchantReference);
            requestDoc.set("Request.Transaction.TxnDetails.amount", payment.Amount.ToString("0.##"));
            requestDoc.set("Request.Transaction.CardTxn.method", "pre");
            requestDoc.setWithAttributes("Request.Transaction.CardTxn.card_details", dataCashReference, new Hashtable() { { "type", "from_hps" } });

            // 3rd Man Service
            requestDoc.setWithAttributes("Request.Transaction.TxnDetails.The3rdMan", string.Empty, new Hashtable() { { "type", "realtime" } });

            // Customer Information
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.customer_reference", DateTime.Now.Ticks.ToString());
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.forename", payment.BillingAddress.FirstName);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.surname", payment.BillingAddress.LastName);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.telephone", payment.BillingAddress.DaytimePhoneNumber);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.email", payment.BillingAddress.Email);
            // for realtime fraud check requests
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.mobile_telephone_number", payment.BillingAddress.DaytimePhoneNumber);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.ip_address", Utilities.GetIPAddress(HttpContext.Current.Request));

            // Delivery address
            var shippingAddress = orderForm.Shipments.First().ShippingAddress;
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.DeliveryAddress.street_address_1", shippingAddress.Line1);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.DeliveryAddress.street_address_2", shippingAddress.Line2);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.DeliveryAddress.city", shippingAddress.City);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.DeliveryAddress.country", CountryCodes.GetNumericCountryCode(shippingAddress.CountryCode));
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.DeliveryAddress.postcode", shippingAddress.PostalCode);

            // Billing address
            var billingAddress = payment.BillingAddress;
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.BillingAddress.street_address_1", billingAddress.Line1);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.BillingAddress.street_address_2", billingAddress.Line2);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.BillingAddress.city", billingAddress.City);
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.BillingAddress.country", CountryCodes.GetNumericCountryCode(billingAddress.CountryCode));
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.BillingAddress.postcode", billingAddress.PostalCode);
            
            // Order information
            var allLineItems = orderForm.GetAllLineItems().ToList();
            requestDoc.setWithAttributes("Request.Transaction.TxnDetails.The3rdMan.OrderInformation.Products", string.Empty, new Hashtable() { { "count", allLineItems.Count.ToString() } });

            foreach (var xmlElement in CreateProductXmFromLineItems(requestDoc, allLineItems, orderGroupCurrency))
            {
                requestDoc.setXmlElement("Request.Transaction.TxnDetails.The3rdMan.OrderInformation.Products.Product", xmlElement);
            }

            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.http_header_fields", GetHeaders());
            // Register the consumer associated with this transaction for the consumer product
            requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.register_consumer_watch", "true");

            // Uncomment this line if you are using full account, since 3rd man fraud checking is not available for test account
            //requestDoc.set("Request.Transaction.TxnDetails.The3rdMan.Realtime.real_time_sha1", HashCode(merchantReference));

            return requestDoc;
        }

        public Document CreateDocumentForFulfillRequest(IPayment payment, IOrderForm orderForm)
        {
            var captureCount = orderForm.Payments.Count(x => x.TransactionType == TransactionType.Capture.ToString());
            var merchantReference = payment.Properties[DataCashPaymentGateway.DataCashMerchantReferencePropertyName] as string;

            var requestDoc = CreateDocument();
            requestDoc.set("Request.Transaction.TxnDetails.merchantreference", $"{merchantReference}_{captureCount}");
            requestDoc.set("Request.Transaction.TxnDetails.amount", payment.Amount.ToString("0.##"));
            requestDoc.set("Request.Transaction.HistoricTxn.method", "fulfill");
            requestDoc.set("Request.Transaction.HistoricTxn.authcode", payment.Properties[DataCashPaymentGateway.DataCashAuthenticateCodePropertyName] as string);
            requestDoc.set("Request.Transaction.HistoricTxn.reference", payment.Properties[DataCashPaymentGateway.DataCashReferencePropertyName] as string);

            return requestDoc;
        }

        public Document CreateDocumentForRefundRequest(IPayment payment)
        {
            var dataCashReference = string.IsNullOrEmpty(payment.ProviderTransactionID) ? payment.TransactionID : payment.ProviderTransactionID;

            var requestDoc = CreateDocument();
            requestDoc.set("Request.Transaction.TxnDetails.amount", payment.Amount.ToString("0.##"));
            requestDoc.set("Request.Transaction.HistoricTxn.method", "txn_refund");
            requestDoc.set("Request.Transaction.HistoricTxn.reference", dataCashReference);

            return requestDoc;
        }

        private Document CreateDocument()
        {
            var requestDoc = new Document(_dataCashConfiguration.Config);
            requestDoc.set("Request.Authentication.client", _dataCashConfiguration.UserId);
            requestDoc.set("Request.Authentication.password", _dataCashConfiguration.Password);

            return requestDoc;
        }

        private IEnumerable<XmlElement> CreateProductXmFromLineItems(Document requestDoc, IEnumerable<ILineItem> lineItems, Currency orderGroupCurrency)
        {
            var currency = new Currency(orderGroupCurrency);

            foreach (var item in lineItems)
            {
                var xmlElement = requestDoc.getXmlDocument().CreateElement("Product");

                var childElement = requestDoc.getXmlDocument().CreateElement("code");
                childElement.InnerText = item.Code;
                xmlElement.AppendChild(childElement);

                childElement = requestDoc.getXmlDocument().CreateElement("quantity");
                childElement.InnerText = ((int)item.Quantity).ToString();
                xmlElement.AppendChild(childElement);

                //recalculate final unit price after all kind of discounts are subtracted from item.ListPrice
                var finalUnitPrice = currency.Round(item.GetExtendedPrice(orderGroupCurrency, _lineItemCalculator).Amount / item.Quantity);
                childElement = requestDoc.getXmlDocument().CreateElement("price");
                childElement.InnerText = finalUnitPrice.ToString("0.##");
                xmlElement.AppendChild(childElement);

                yield return xmlElement;
            }
        }

        private string GetHeaders()
        {
            // Comma separated list of original http headers. The headers of most interest are Referrer, Host, Server, User-Agent, Via and X-Forwarded-For, max 200 chars
            var headers = HttpContext.Current.Request.Headers;
            var headerToGet = new [] { "Referrer", "Host", "Server", "User-Agent", "Via", "X-Forwarded-For" };
            var headersDictionary = headers.Cast<string>().Select(headerKey => new { Key = headerKey, Value = headers[headerKey] }).
                                    Where(header => headerToGet.Any(item => item.Equals(header.Key, StringComparison.OrdinalIgnoreCase)));
            var comma = ",";
            var headersString = string.Join(comma, (from item in headersDictionary select $"{item.Key}:{item.Value}").ToArray());
            if (headersString.Length > 200)
            {
                headersString = headersString.Substring(0, 200).Substring(0, headersString.LastIndexOf(comma, StringComparison.InvariantCulture));
            }

            return headersString;
        }
    }
}