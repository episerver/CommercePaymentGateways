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

            Assert.True(result.ContainsKey("paymentprovider"));
            Assert.True(result.ContainsKey("merchant"));
            Assert.Equal("SampleMerchant", result["merchant"]);
            Assert.True(result.ContainsKey("amount"));
            Assert.Equal(Utilities.GetAmount(currency, _payment.Amount), result["amount"]);
            Assert.True(result.ContainsKey("currency"));
            Assert.Equal(currency.ToString(), result["currency"]);
            Assert.True(result.ContainsKey("orderid"));
            Assert.Equal(_orderNumber, result["orderid"]);
            Assert.True(result.ContainsKey("uniqueoid"));
            Assert.True(result.ContainsKey("accepturl"));
            Assert.Equal(notifyUrl, result["accepturl"]);
            Assert.True(result.ContainsKey("cancelurl"));
            Assert.Equal(notifyUrl, result["cancelurl"]);
            Assert.True(result.ContainsKey("lang"));
            Assert.True(result.ContainsKey("md5key"));
            Assert.Equal(notifyUrl, result["cancelurl"]);
        }

        string _orderNumber = "PO123";
        IPayment _payment;
        IOrderGroup _orderGroup;
        DIBSRequestHelper _subject;

        public DIBSRequestHelperTests()
        {
            _payment = FakePayment.CreatePayment(125m, PaymentType.CreditCard, Guid.NewGuid());

            var orderForm = FakeOrderForm.CreateOrderForm();
            var shipment = new FakeShipment();

            orderForm.Shipments.Add(shipment);
            orderForm.Payments.Add(_payment);

            _orderGroup = new FakeOrderGroup();
            _orderGroup.Forms.Add(orderForm);
            
            var dibsConfiguration = new DIBSConfigurationForTest();

            var orderNumberGeneratorMock = new Mock<IOrderNumberGenerator>();
            orderNumberGeneratorMock.Setup(x => x.GenerateOrderNumber(It.IsAny<IOrderGroup>())).Returns(_orderNumber);

            _subject = new DIBSRequestHelper(orderNumberGeneratorMock.Object, new SiteContext(), dibsConfiguration);
        }

        class DIBSConfigurationForTest : DIBSConfiguration
        {
            protected override void Initialize(IDictionary<string, string> settings)
            {
                PaymentMethodId = Guid.NewGuid();

                Merchant = "SampleMerchant";
                Password = "samplePassword";

                ProcessingUrl = "http://sampledibs.com";
                MD5Key1 = "sampleKey1";
                MD5Key2 = "sampleKey2";
            }
        }
    }
}
