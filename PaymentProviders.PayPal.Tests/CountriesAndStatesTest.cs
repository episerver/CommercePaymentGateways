using PayPal.PayPalAPIInterfaceService.Model;
using Xunit;

namespace PaymentProviders.PayPal.Tests
{
    public class CountriesAndStatesTest
    {
        [InlineData("US", "USA")]
        [InlineData("XX", "")]
        [Theory]
        public void GetAlpha3CountryCodeTest(string iso2code, string expected)
        {
            var result = CountriesAndStates.GetAlpha3CountryCode(iso2code);

            Assert.Equal(expected, result);
        }

        [InlineData("USA", CountryCodeType.US)]
        [InlineData("XXX", CountryCodeType.CUSTOMCODE)]
        [Theory]
        public void GetAlpha2CountryCodeTest(string iso3code, CountryCodeType expected)
        {
            var result = CountriesAndStates.GetAlpha2CountryCode(iso3code);

            Assert.Equal(expected, result);
        }

        [InlineData("CA", "California")]
        [InlineData("", "")]
        [InlineData("XX", "XX")]
        [Theory]
        public void GetStateName_Test(string stateCode, string expected)
        {
            var result = CountriesAndStates.GetStateName(stateCode);

            Assert.Equal(expected, result);
        }
        
        [InlineData("California", "CA")]
        [InlineData("XXX", "XXX")]
        [InlineData("", "")]
        [Theory]
        public void GetStateCodeTest(string stateName, string expected)
        {
            var result = CountriesAndStates.GetStateCode(stateName);

            Assert.Equal(expected, result);
        }
    }
}
