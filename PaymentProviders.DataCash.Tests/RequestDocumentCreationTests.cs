using DataCash;
using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Moq;
using PaymentProviders.DataCash.Tests.TestSupport;
using System;
using System.IO;
using System.Linq;
using System.Web;
using Xunit;

namespace PaymentProviders.DataCash.Tests
{
    public class RequestDocumentCreationTests
    {
        [Fact]
        public void CreateDocumentForPaymentCheckout_ShouldReturnDataCashDocument()
        {
            var notifyUrl = "http://localhost:5432/DataCashPaymentProcessingPage";
            var result = _subject.CreateDocumentForPaymentCheckout(_cart, _payment, notifyUrl);

            Assert.Equal("UserId", result.get("Request.Authentication.client"));
            Assert.Equal("Password", result.get("Request.Authentication.password"));

            Assert.Equal("DataCashMerchantReferencePropertyValue", result.get("Request.Transaction.TxnDetails.merchantreference"));
            Assert.Equal("setup", result.get("Request.Transaction.HpsTxn.method"));
            Assert.Equal("PaymentPageId", result.get("Request.Transaction.HpsTxn.page_set_id"));

            Assert.Equal(notifyUrl, result.get("Request.Transaction.HpsTxn.return_url"));
        }

        [Fact]
        public void CreateDocumentForPreAuthenticateRequest_ShouldReturnDataCashDocument()
        {
            HttpContext.Current = new HttpContext(
                new HttpRequest("", "http://tempuri.org", ""),
                new HttpResponse(new StringWriter())
                );

            var result = _subject.CreateDocumentForPreAuthenticateRequest(_payment, _cart.Forms.First(), _cart.Currency);

            Assert.Equal("UserId", result.get("Request.Authentication.client"));
            Assert.Equal("Password", result.get("Request.Authentication.password"));

            Assert.Equal("DataCashMerchantReferencePropertyValue", result.get("Request.Transaction.TxnDetails.merchantreference"));
            Assert.Equal("125", result.get("Request.Transaction.TxnDetails.amount"));
            Assert.Equal("pre", result.get("Request.Transaction.CardTxn.method"));
            Assert.Equal("DataCashReferencePropertyValue", result.get("Request.Transaction.CardTxn.card_details"));

            Assert.Equal(_payment.BillingAddress.FirstName, result.get("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.forename"));
            Assert.Equal(_payment.BillingAddress.LastName, result.get("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.surname"));
            Assert.Equal(_payment.BillingAddress.DaytimePhoneNumber, result.get("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.telephone"));
            Assert.Equal(_payment.BillingAddress.Email, result.get("Request.Transaction.TxnDetails.The3rdMan.CustomerInformation.email"));
            
            Assert.Equal("true", result.get("Request.Transaction.TxnDetails.The3rdMan.register_consumer_watch"));
        }

        [Fact]
        public void CreateDocumentForFulfillRequest_ShouldReturnDataCashDocument()
        {
            var result = _subject.CreateDocumentForFulfillRequest(_payment, _cart.Forms.First());

            Assert.Equal("UserId", result.get("Request.Authentication.client"));
            Assert.Equal("Password", result.get("Request.Authentication.password"));

            Assert.Equal("DataCashMerchantReferencePropertyValue_0", result.get("Request.Transaction.TxnDetails.merchantreference"));
            Assert.Equal("125", result.get("Request.Transaction.TxnDetails.amount"));
            Assert.Equal("fulfill", result.get("Request.Transaction.HistoricTxn.method"));
            Assert.Equal("DataCashAuthenticateCodePropertyValue", result.get("Request.Transaction.HistoricTxn.authcode"));
            Assert.Equal("DataCashReferencePropertyValue", result.get("Request.Transaction.HistoricTxn.reference"));
        }

        [Fact]
        public void CreateDocumentForRefundRequest_ShouldReturnDataCashDocument()
        {
            var result = _subject.CreateDocumentForRefundRequest(_payment);

            Assert.Equal("UserId", result.get("Request.Authentication.client"));
            Assert.Equal("Password", result.get("Request.Authentication.password"));

            Assert.Equal("125", result.get("Request.Transaction.TxnDetails.amount"));
            Assert.Equal("txn_refund", result.get("Request.Transaction.HistoricTxn.method"));
            Assert.Equal("ProviderTransactionIDValue", result.get("Request.Transaction.HistoricTxn.reference"));
        }

        RequestDocumentCreation _subject;
        ICart _cart;
        IPayment _payment;

        public RequestDocumentCreationTests()
        {
            _payment = FakePayment.CreatePayment(125m, PaymentType.CreditCard, Guid.NewGuid());
            _payment.Properties[DataCashPaymentGateway.DataCashReferencePropertyName] = "DataCashReferencePropertyValue";
            _payment.Properties[DataCashPaymentGateway.DataCashAuthenticateCodePropertyName] = "DataCashAuthenticateCodePropertyValue";
            _payment.Properties[DataCashPaymentGateway.DataCashMerchantReferencePropertyName] = "DataCashMerchantReferencePropertyValue";
            _payment.ProviderTransactionID = "ProviderTransactionIDValue";

            var orderForm = FakeOrderForm.CreateOrderForm();
            var shipment = new FakeShipment();
            shipment.ShippingAddress = FakeOrderAddress.CreateOrderAddress();

            orderForm.Shipments.Add(shipment);
            orderForm.Payments.Add(_payment);

            _cart = new FakeOrderGroup();
            _cart.Forms.Add(orderForm);

            var _orderGroupCalculatorMock = new Mock<IOrderGroupCalculator>();
            _orderGroupCalculatorMock.Setup(x => x.GetTotal(It.IsAny<IOrderGroup>())).Returns(new Money(125m, Currency.USD));
            var _lineItemCalculatorMock = new Mock<ILineItemCalculator>();
            _lineItemCalculatorMock.Setup(x => x.GetExtendedPrice(It.IsAny<ILineItem>(), It.IsAny<Currency>())).Returns(new Money(115.33m, Currency.USD));
            
            _subject = new RequestDocumentCreation(_orderGroupCalculatorMock.Object, _lineItemCalculatorMock.Object, new FakeDataCashConfiguration());
        }

        class FakeDataCashConfiguration : DataCashConfiguration
        {
            protected override void Inittialize()
            {
                UserId = "UserId";
                Password = "Password";
                PaymentPageId = "PaymentPageId";
                Config = new Config();
            }
        }
    }
}