using Xunit;

namespace PaymentProviders.DataCash.Tests
{
    public class CountryCodesTests
    {
        [InlineData("ALB", "8")]
        [InlineData("USA", "840")]
        [InlineData("ZZZ", "")]
        [Theory]
        public void GetNumericCountryCode_ShouldReturnCorrect(string countryCode, string expected)
        {
            var result = CountryCodes.GetNumericCountryCode(countryCode);

            Assert.Equal(expected, result);
        }
    }
}