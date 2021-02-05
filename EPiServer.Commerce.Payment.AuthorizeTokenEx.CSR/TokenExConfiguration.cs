using EPiServer.Commerce.UI.CustomerService.Extensibility;
using EPiServer.ServiceLocation;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR
{
    [ServiceConfiguration(typeof(CSRUIExtensionConfiguration))]
    public class TokenExConfiguration : CSRUIExtensionConfiguration
    {
        public TokenExConfiguration() => ResourceScripts = new string[]
            {
                "/CSRExtensibility/react-app/dist/tokenExPayment.min.js",
                "https://htp.tokenex.com/Iframe/iframe-v3.min.js"
            };
    }
}