using EPiServer.Business.Commerce.Payment.DIBS.Tests.TestSupport;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace EPiServer.Business.Commerce.Payment.DIBS.Tests
{
    public class DIBSRequestHelperTests
    {
        [Fact]
        public void CreateRequestPaymentData_ShouldReturnCorrect()
        {
            var notifyUrl = "http://episervercommerce/dibscheckout";
            var currency = _orderGroup.Currency;
            var result = _subject.CreateRequestPaymentData(_payment, _orderGroup as ICart, notifyUrl);
            var billingAddress = _payment.BillingAddress;

            Assert.True(result.ContainsKey("acceptReturnUrl"));
            Assert.Equal(notifyUrl, result["acceptReturnUrl"]);

            Assert.True(result.ContainsKey("amount"));
            Assert.Equal("12500", result["amount"].ToString());

            Assert.True(result.ContainsKey("billingAddress"));
            Assert.Equal(billingAddress.Line1, result["billingAddress"].ToString());

            Assert.True(result.ContainsKey("billingAddress2"));
            Assert.Equal(billingAddress.Line2, result["billingAddress2"].ToString());

            Assert.True(result.ContainsKey("billingEmail"));
            Assert.Equal(billingAddress.Email, result["billingEmail"].ToString());

            Assert.True(result.ContainsKey("billingFirstName"));
            Assert.Equal(billingAddress.FirstName, result["billingFirstName"].ToString());

            Assert.True(result.ContainsKey("billingLastName"));
            Assert.Equal(billingAddress.LastName, result["billingLastName"].ToString());

            Assert.True(result.ContainsKey("billingMobile"));
            Assert.Equal(billingAddress.DaytimePhoneNumber, result["billingMobile"].ToString());

            Assert.True(result.ContainsKey("billingPostalCode"));
            Assert.Equal(billingAddress.PostalCode, result["billingPostalCode"].ToString());

            Assert.True(result.ContainsKey("cancelReturnUrl"));
            Assert.Equal(notifyUrl, result["cancelReturnUrl"].ToString());

            Assert.True(result.ContainsKey("currency"));
            Assert.Equal(_orderGroup.Currency.CurrencyCode, result["currency"].ToString());

            Assert.True(result.ContainsKey("merchant"));
            Assert.Equal(_dibsConfiguration.Merchant, result["merchant"].ToString());

            Assert.True(result.ContainsKey("orderId"));

            Assert.True(result.ContainsKey("test"));
            Assert.Equal("1", result["test"].ToString());
        }

        private readonly string _orderNumber = "PO123";
        private IPayment _payment;
        private IOrderGroup _orderGroup;
        private DIBSRequestHelperForTest _subject;
        private DIBSConfiguration _dibsConfiguration;

        public DIBSRequestHelperTests()
        {
            _payment = FakePayment.CreatePayment(125m, PaymentType.CreditCard, Guid.NewGuid());

            var orderForm = FakeOrderForm.CreateOrderForm();
            var shipment = new FakeShipment();

            orderForm.Shipments.Add(shipment);
            orderForm.Payments.Add(_payment);

            _orderGroup = new FakeOrderGroup();
            _orderGroup.Forms.Add(orderForm);

            _dibsConfiguration = new DIBSConfigurationForTest();

            var orderNumberGeneratorMock = new Mock<IOrderNumberGenerator>();
            orderNumberGeneratorMock.Setup(x => x.GenerateOrderNumber(It.IsAny<IOrderGroup>())).Returns(_orderNumber);

            _subject = new DIBSRequestHelperForTest(orderNumberGeneratorMock.Object, new SiteContext(), _dibsConfiguration);
        }

        private class DIBSConfigurationForTest : DIBSConfiguration
        {
            protected override void Initialize(IDictionary<string, string> settings)
            {
                PaymentMethodId = Guid.NewGuid();

                Merchant = "SampleMerchant";
                Password = "samplePassword";

                ProcessingUrl = "http://sampledibs.com";
                HMACKey = "sampleKey1";
            }
        }

        private class DIBSRequestHelperForTest : DIBSRequestHelper
        {
            public DIBSRequestHelperForTest(
                IOrderNumberGenerator orderNumberGenerator,
                SiteContext siteContext,
                DIBSConfiguration dibsConfiguration) : base(orderNumberGenerator, siteContext, dibsConfiguration)
            { }

            protected override string GetMACRequest(DIBSConfiguration configuration, Dictionary<string, object> message)
            {
                return "sample generated MAC";
            }
        }
    }
}
