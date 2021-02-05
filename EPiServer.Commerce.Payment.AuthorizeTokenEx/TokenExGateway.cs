using EPiServer.Commerce.Order.Payments.Tokenization;
using EPiServer.ServiceLocation;
using System;
using System.Net.Http;
using System.Text;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx
{
    /// <summary>
    /// Implements operations for TokenEx gateway.
    /// </summary>
    [ServiceConfiguration(typeof(ITokenizationGateway))]
    public class TokenExGateway : ITokenizationGateway
    {
        private readonly string _transactionUrlHeaderName = "TX_URL";
        private readonly string _tokenExIdHeaderName = "TX_TokenExID";
        private readonly string _apiKeyHeaderName = "TX_APIKey";
        private readonly string _testDetokenizeUrl = "https://test-api.tokenex.com/TransparentGatewayAPI/Detokenize";
        private readonly string _productionDetokenizeUrl = "https://api.tokenex.com/TransparentGatewayAPI/Detokenize";

        private readonly TokenizationOptions _tokenizationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenExGateway" /> class.
        /// </summary>
        public TokenExGateway() : this(ServiceLocator.Current.GetInstance<TokenizationOptions>())
        { }

        protected TokenExGateway(TokenizationOptions tokenizationOptions) => _tokenizationOptions = tokenizationOptions;

        /// <inheritdoc />
        public DetokenizedReponse Send(TokenizedRequest request)
        {
            HttpResponseMessage httpResponse;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(_transactionUrlHeaderName, request.Endpoint);
                client.DefaultRequestHeaders.Add(_tokenExIdHeaderName, _tokenizationOptions.TokenizationId);
                client.DefaultRequestHeaders.Add(_apiKeyHeaderName, _tokenizationOptions.ApiKey);

                var payload = new StringContent(request.Data, Encoding.UTF8, request.DataFormat);
                httpResponse = client.PostAsync(new Uri(GetDetokenizeUrl(_tokenizationOptions.TestMode)), payload).Result;
            }

            return new DetokenizedReponse()
            {
                Content = httpResponse.Content.ReadAsStringAsync().Result,
                StatusCode = httpResponse.StatusCode.ToString(),
                IsSuccess = httpResponse.IsSuccessStatusCode,
                Message = httpResponse.ReasonPhrase
            };
        }

        private string GetDetokenizeUrl(bool testMode) => testMode ? _testDetokenizeUrl : _productionDetokenizeUrl;
    }
}