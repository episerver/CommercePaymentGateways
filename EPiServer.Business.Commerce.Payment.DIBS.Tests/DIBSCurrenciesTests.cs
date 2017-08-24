using Mediachase.Commerce;
using Xunit;

namespace EPiServer.Business.Commerce.Payment.DIBS.Tests
{
    public class DIBSCurrenciesTests
    {
        [Fact]
        public void GetCurrencyCode_WhenCodeIsSupported_ShouldReturnISO4217NumberCorrectly()
        {
            var result = DIBSCurrencies.GetCurrencyCode(new Currency("USD"));

            Assert.Equal("840", result);
        }

        [Fact]
        public void GetCurrencyCode_WhenCodeIsNotSupported_ShouldReturnEmpty()
        {
            var result = DIBSCurrencies.GetCurrencyCode(new Currency("XXX"));

            Assert.Equal(string.Empty, result);
        }
    }
}
