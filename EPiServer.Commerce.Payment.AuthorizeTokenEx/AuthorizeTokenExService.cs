using AuthorizeNet;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers.Bases;
using EPiServer.Business.Commerce.Plugins.Payment.Authorize;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Payments.Tokenization;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Plugins.Payment.Authorize;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using createCustomerPaymentProfileRequest = AuthorizeNet.Api.Contracts.V1.createCustomerPaymentProfileRequest;
using createCustomerPaymentProfileResponse = AuthorizeNet.Api.Contracts.V1.createCustomerPaymentProfileResponse;
using creditCardType = AuthorizeNet.Api.Contracts.V1.creditCardType;
using customerPaymentProfileType = AuthorizeNet.Api.Contracts.V1.customerPaymentProfileType;
using paymentType = AuthorizeNet.Api.Contracts.V1.paymentType;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx
{
    /// <summary>
    /// Contains all the functions needed to perform operations of Authorize integrated with TokenEx.
    /// </summary>
    public class AuthorizeTokenExService : AuthorizeNetService
    {
        private readonly ITokenizationGateway _tokenService;

        public AuthorizeTokenExService()
        {
        }

        public AuthorizeTokenExService(TransactionRequest transactionRequest, ITokenizationGateway tokenizationService) : base(transactionRequest)
        {
            _tokenService = tokenizationService;
        }

        public override string AddCreditCard(string customerProfileId, string cardNumber, int expMonth, int expYear,
            string cscNumber, AuthorizeNet.Address address)
        {
            return AddCreditCard(customerProfileId, cardNumber, expMonth, expYear);
        }

        public Customer CreateCustomer(string invoiceNumber, IOrderAddress address)
        {
            //Due to the limitations in API and error handling from authorized.net,
            //this is the only way I found to create or get customers if already exists. 
            try
            {
                return Gateway.CreateCustomer(address.Email,
                $"{address.FirstName} {address.LastName}", invoiceNumber);
            }
            catch (System.Exception ex)
            {
                if (!ex.Message.Contains("E00039"))
                {
                    throw new PaymentException(PaymentException.ErrorType.ProviderError, string.Empty, ex.Message, ex);
                }
                //gets the customer id when it's duplicated
                var profileId = Regex.Match(ex.Message, @"ID (\d+)").Groups[1].Value;
                return GetCustomer(profileId);
            }
        }

        public AuthorizeNet.Api.Contracts.V1.merchantAuthenticationType GetMerchantAuthenticationType()
        {
            ApiOperationBase<AuthorizeNet.Api.Contracts.V1.ANetApiRequest, AuthorizeNet.Api.Contracts.V1.ANetApiResponse>
                .RunEnvironment = TransactionRequest.TestMode ? Environment.SANDBOX : Environment.PRODUCTION;
            return new AuthorizeNet.Api.Contracts.V1.merchantAuthenticationType
            {
                name = TransactionRequest.User,
                Item = TransactionRequest.Password,
                ItemElementName = ItemChoiceType.transactionKey
            };
        }

        private TokenizedRequest CreateDetokenizeRequest(string customerProfileId, string token, int expMonth, int expYear)
        {
            var endpoint = TransactionRequest.TestMode ? HttpXmlUtility.TEST_URL : HttpXmlUtility.URL;
            var paymentProfile = new customerPaymentProfileType
            {
                payment = new paymentType
                {
                    Item = new creditCardType
                    {
                        cardNumber = FormatToken(token),
                        cardCode = FormatCVV,
                        expirationDate = $"{expYear:D2}{expMonth:D2}"
                    }
                }
            };

            var transactionRequest = new createCustomerPaymentProfileRequest
            {
                merchantAuthentication = GetMerchantAuthenticationType(),
                customerProfileId = customerProfileId,
                paymentProfile = paymentProfile
            };

            var serializer = new XmlSerializer(typeof(createCustomerPaymentProfileRequest));
            using (var content = new StringWriter())
            {
                using (var writer = XmlWriter.Create(content))
                {
                    serializer.Serialize(writer, transactionRequest);
                    return new TokenizedRequest()
                    {
                        Data = content.ToString(),
                        DataFormat = "text/xml",
                        Endpoint = endpoint
                    };
                }
            }
        }

        /// <summary>
        /// Gets the formatted token value for card number.
        /// </summary>
        /// <param name="tokenValue">The token of card number.</param>
        private string FormatToken(string tokenValue) => "{{{" + tokenValue + "}}}";

        /// <summary>
        /// Gets formatted cvv value.
        /// </summary>
        /// <returns>The default cvv value for tokenEx.</returns>
        private string FormatCVV => "{{{CVV}}}";

        private string AddCreditCard(string customerProfileId, string token, int expMonth, int expYear)
        {
            var detokenizeRequestData = CreateDetokenizeRequest(customerProfileId, token, expMonth, expYear);
            var detokenizeResponseData = _tokenService.Send(detokenizeRequestData);

            if (!detokenizeResponseData.IsSuccess)
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, detokenizeResponseData.StatusCode, detokenizeResponseData.Message);
            }

            var xmls = new XmlSerializer(typeof(createCustomerPaymentProfileResponse));
            var paymentProfileResponse = (createCustomerPaymentProfileResponse)xmls.Deserialize(new StringReader(detokenizeResponseData.Content));

            return paymentProfileResponse.customerPaymentProfileId;

        }
    }
}