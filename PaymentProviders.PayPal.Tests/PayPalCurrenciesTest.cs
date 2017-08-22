using Mediachase.Commerce;
using Mediachase.Commerce.Core;
using PayPal.PayPalAPIInterfaceService.Model;
using Xunit;

namespace PaymentProviders.PayPal.Tests
{
    public class PayPalCurrenciesTest
    {
        [InlineData("USD", "USD", CurrencyCodeType.USD)]
        [InlineData("EUR", null, CurrencyCodeType.EUR)]
        [InlineData("XXX", null, CurrencyCodeType.CUSTOMCODE)]
        [InlineData("USD", "XXX", CurrencyCodeType.CUSTOMCODE)]
        [Theory]
        public void GetCurrencyCode_ShouldReturnCorrectly(string siteContextCurrency, string currencyCode, CurrencyCodeType expected)
        {
            var subject = CreateSubject(siteContextCurrency);
            var currency = !string.IsNullOrEmpty(currencyCode) ? new Currency(currencyCode) : null;
            var result = subject.GetCurrencyCode(currency);

            Assert.Equal(expected, result);
        }

        private PayPalCurrencies CreateSubject(string currencyCode)
        {
            var siteContext = new SiteContext();
            siteContext.Currency = new Currency(currencyCode);
            return new PayPalCurrencies(siteContext);
        }
    }
}
