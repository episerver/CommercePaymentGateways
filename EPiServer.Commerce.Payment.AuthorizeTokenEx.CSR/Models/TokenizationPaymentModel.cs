using EPiServer.Commerce.Order.Payments.Tokenization;
using EPiServer.Commerce.UI.CustomerService.Models;
using Mediachase.Commerce;
using System.ComponentModel.DataAnnotations;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR.Models
{
    public class TokenizationPaymentModel : PaymentModel
    {
        public TokenizationPaymentModel()
        {
        }

        public TokenizationPaymentModel(ITokenizedPayment payment, Currency orderGroupCurrency)
            : base(payment, orderGroupCurrency)
        {
            Token = payment.Token;
            ExpirationMonth = payment.ExpirationMonth;
            ExpirationYear = payment.ExpirationYear;
        }

        public string Token { get; set; }

        [Range(1, 12, ErrorMessage = "Expiration month must be between 1 and 12.")]
        public int ExpirationMonth { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Expiration year must be positive value.")]
        public int ExpirationYear { get; set; }
    }
}