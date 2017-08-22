using Xunit;

namespace PaymentProviders.DIBS.Tests
{
    public class DIBSLanguagesTests
    {
        [Fact]
        public void GetCurrentDIBSSupportedLanguage_WhenLanguageIsSupported_ShouldReturnCodeCorrectly()
        {
            var result = DIBSLanguages.GetCurrentDIBSSupportedLanguage("Swedish");

            Assert.Equal("sv", result);
        }

        [Fact]
        public void GetCurrentDIBSSupportedLanguage_WhenLanguageIsNotSupported_ShouldReturnEnglishCodeByDefault()
        {
            var result = DIBSLanguages.GetCurrentDIBSSupportedLanguage("XXX");

            Assert.Equal("en", result);
        }
    }
}
