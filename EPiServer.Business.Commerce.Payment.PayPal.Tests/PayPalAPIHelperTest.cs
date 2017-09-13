using EPiServer.Business.Commerce.Payment.PayPal.Tests.TestSupport;
using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders;
using Moq;
using PayPal.PayPalAPIInterfaceService.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace EPiServer.Business.Commerce.Payment.PayPal.Tests
{
    public class PayPalAPIHelperTest
    {
        [InlineData(AckCodeType.SUCCESS)]
        [InlineData(AckCodeType.SUCCESSWITHWARNING)]
        [Theory]
        public void CheckErrors_WhenAckCodeIsSuccess_ShouldReturnEmpty(AckCodeType ackCode)
        {
            var abstractResponseType = new AbstractResponseType { Ack = ackCode };
            var result = _subject.CheckErrors(abstractResponseType);

            Assert.Equal(string.Empty, result);
        }

        [InlineData(AckCodeType.FAILURE)]
        [InlineData(AckCodeType.FAILUREWITHWARNING)]
        [InlineData(AckCodeType.PARTIALSUCCESS)]
        [InlineData(AckCodeType.WARNING)]
        [InlineData(AckCodeType.CUSTOMCODE)]
        [Theory]
        public void CheckErrors_WhenAckCodeIsNotSuccessAndResponseHasErrors_ShouldReturnCorrectly(AckCodeType ackCode)
        {
            var errorCode = "E0001";
            var message = "ThisIsVeryLongMessage";
            var abstractResponseType = new AbstractResponseType { Ack = ackCode };
            abstractResponseType.Errors.Add(new ErrorType { SeverityCode = SeverityCodeType.ERROR, ErrorCode = errorCode, LongMessage = message });
            var result = _subject.CheckErrors(abstractResponseType);

            Assert.True(result.Contains(SeverityCodeType.ERROR.ToString()));
            Assert.True(result.Contains(errorCode));
            Assert.True(result.Contains(message));
        }

        [InlineData(AckCodeType.FAILURE)]
        [InlineData(AckCodeType.FAILUREWITHWARNING)]
        [InlineData(AckCodeType.PARTIALSUCCESS)]
        [InlineData(AckCodeType.WARNING)]
        [InlineData(AckCodeType.CUSTOMCODE)]
        [Theory]
        public void CheckErrors_WhenAckCodeIsNotSuccessAndResponseHasNoErrors_ShouldReturnCorrectly(AckCodeType ackCode)
        {
            var abstractResponseType = new AbstractResponseType { Ack = ackCode };
            var result = _subject.CheckErrors(abstractResponseType);

            Assert.True(!string.IsNullOrEmpty(result));
        }

        [InlineData(TransactionType.Authorization, false, false, PaymentActionCodeType.AUTHORIZATION, "1", null, null)]
        [InlineData(TransactionType.Sale, false, false, PaymentActionCodeType.SALE, "1", null, null)]
        [InlineData(TransactionType.Sale, true, false, PaymentActionCodeType.SALE, null, null, null)]
        [InlineData(TransactionType.Sale, true, true, PaymentActionCodeType.SALE, null, SolutionTypeType.SOLE, LandingPageType.BILLING)]
        [InlineData(TransactionType.Other, false, false, null, "1", null, null)]
        [Theory]
        public void CreateExpressCheckoutReqDetailsType_ShouldReturnCorrectly(TransactionType transType, bool allowChangeAddress, bool allowGuest,
            PaymentActionCodeType? expectedAction, string expectedAddressOverride, SolutionTypeType? expectedSolutionType, LandingPageType? expectedLandingPageType)
        {
            var payment = new FakePayment();
            payment.TransactionType = transType.ToString();
            payment.BillingAddress = FakeOrderAddress.CreateOrderAddress();

            var config = new PayPalConfigurationForTest();
            config.Setup(allowChangeAddress, allowGuest);

            var result = _subject.CreateExpressCheckoutReqDetailsType(payment, config);

            Assert.Equal(payment.BillingAddress.Email, result.BuyerEmail);
            Assert.Equal(expectedAction, result.PaymentAction);
            Assert.Equal(expectedAddressOverride, result.AddressOverride);
            Assert.Equal(expectedSolutionType, result.SolutionType);
            Assert.Equal(expectedLandingPageType, result.LandingPage);
        }

        [Fact]
        public void GetPaymentDetailsType_ShouldReturnCorrectly()
        {
            var itemCode = "TestCode";
            var itemName = "TestName";
            var orderNumber = "PO9999";
            var notifyUrl = "NotifyURL";
            var itemPrice = 100m;
            var quantiy = 1;
            var taxAmount = 20m;
            var shippingCost = 10m;
            var orderTotal = itemPrice * quantiy + taxAmount + shippingCost;

            var factory = new FakeOrderGroupBuilderFactory();
            var orderForm = factory.CreateOrderForm();
            var shipment = factory.CreateShipment();
            var lineItem = factory.CreateLineItem(itemCode);
            lineItem.DisplayName = itemName;
            lineItem.Quantity = quantiy;
            lineItem.PlacedPrice = itemPrice;
            shipment.LineItems.Add(lineItem);

            shipment.ShippingAddress = FakeOrderAddress.CreateOrderAddress();
            orderForm.Shipments.Add(shipment);

            var payment = factory.CreatePayment();
            payment.Amount = orderTotal;
            orderForm.Payments.Add(payment);
            var orderGroup = new FakeOrderGroup();
            orderGroup.Forms.Add(orderForm);

            _taxCalculatorMock.Setup(s => s.GetTaxTotal(It.IsAny<IOrderForm>(), It.IsAny<IMarket>(), Currency.USD)).Returns(new Money(taxAmount, Currency.USD));
            _shippingCalculatorMock.Setup(s => s.GetShippingCost(It.IsAny<IOrderForm>(), It.IsAny<IMarket>(), Currency.USD)).Returns(new Money(shippingCost, Currency.USD));
            var lineItemCalculatorMock = new Mock<ILineItemCalculator>();
            lineItemCalculatorMock.Setup(s => s.GetExtendedPrice(It.IsAny<ILineItem>(), It.IsAny<Currency>())).Returns(new Money(itemPrice, Currency.USD));
            var serviceLocatorMock = new Mock<IServiceLocator>();
            serviceLocatorMock.Setup(s => s.GetInstance<ILineItemCalculator>()).Returns(lineItemCalculatorMock.Object);
            ServiceLocator.SetLocator(serviceLocatorMock.Object);

            var result = _subject.GetPaymentDetailsType(payment, orderGroup, orderNumber, notifyUrl);

            Assert.Equal("Episerver_Cart_EC", result.ButtonSource);
            Assert.Equal(orderNumber, result.InvoiceID);
            Assert.Equal(notifyUrl, result.NotifyURL);
            Assert.Equal(orderTotal.ToString("0.00", CultureInfo.InvariantCulture), result.OrderTotal.value);
            Assert.Equal(0m.ToString("0.00", CultureInfo.InvariantCulture), result.ShippingTotal.value);
            Assert.Equal(0m.ToString("0.00", CultureInfo.InvariantCulture), result.HandlingTotal.value);
            Assert.Equal(taxAmount.ToString("0.00", CultureInfo.InvariantCulture), result.TaxTotal.value);
            Assert.Equal((itemPrice * quantiy + shippingCost).ToString("0.00", CultureInfo.InvariantCulture), result.ItemTotal.value);
            Assert.Equal((itemPrice * quantiy).ToString("0.00", CultureInfo.InvariantCulture), result.PaymentDetailsItem.First().Amount.value);
            Assert.Equal(itemCode, result.PaymentDetailsItem.First().Number);
            Assert.Equal(quantiy, result.PaymentDetailsItem.First().Quantity);
            Assert.Equal(itemName, result.PaymentDetailsItem.First().Name);
        }

        private Mock<ITaxCalculator> _taxCalculatorMock;
        private Mock<IShippingCalculator> _shippingCalculatorMock;
        private Mock<LocalizationService> _localizationServiceMock;

        private PayPalAPIHelper _subject;

        public PayPalAPIHelperTest()
        {
            _taxCalculatorMock = new Mock<ITaxCalculator>();
            _shippingCalculatorMock = new Mock<IShippingCalculator>();
            _localizationServiceMock = new Mock<LocalizationService>(new object[] { null });

            _subject = new PayPalAPIHelper(_shippingCalculatorMock.Object, _taxCalculatorMock.Object, _localizationServiceMock.Object, new PayPalCurrencies(new SiteContext()));
        }

        class PayPalConfigurationForTest : PayPalConfiguration
        {
            protected override void Initialize(IDictionary<string, string> settings)
            {
            }

            public void Setup(bool allowChangeAddress, bool allowGuest)
            {
                AllowChangeAddress = allowChangeAddress ? "1" : "0";
                AllowGuest = allowGuest ? "1" : "0";
            }
        }
    }
}
