using EPiServer.Commerce.Order.Payments.Tokenization;
using EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR.Models;
using EPiServer.Commerce.UI.CustomerService.Extensibility;
using EPiServer.Commerce.UI.CustomerService.Routing;
using EPiServer.ServiceLocation;
using System;
using System.Linq;
using System.Text;
using System.Web.Http;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR.Controllers
{
    [EpiRoutePrefix("tokenEx")]
    public class TokenExApiController : CSRAPIController
    {
        private readonly TokenizationOptions _tokenizationOptions;

        public TokenExApiController()
         : this(ServiceLocator.Current.GetInstance<TokenizationOptions>()
               )
        { }

        public TokenExApiController(
            TokenizationOptions tokenizationOptions)
        {
            _tokenizationOptions = tokenizationOptions;
        }

        [HttpPost]
        [EpiRoute("config")]
        public IHttpActionResult GetTokenExConfig()
        {
            if (!Request.Headers.TryGetValues("Origin", out var originValues))
            {
                return BadRequest("Invalid request.");
            }

            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var tokenScheme = "nTOKEN";

            var concatenatedInfo = $"{_tokenizationOptions.TokenizationId}|{originValues.First()}|{timeStamp}|{tokenScheme}";
            var hmac = new System.Security.Cryptography.HMACSHA256
            {
                Key = Encoding.UTF8.GetBytes(_tokenizationOptions.ClientSecretKey)
            };
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenatedInfo));
            var authenticationKey = Convert.ToBase64String(hash);

            return Ok(new TokenExIframeConfigModel
            {
                TokenExID = _tokenizationOptions.TokenizationId,
                AuthenticationKey = authenticationKey,
                Timestamp = timeStamp,
                TokenScheme = tokenScheme
            });
        }
    }
}
