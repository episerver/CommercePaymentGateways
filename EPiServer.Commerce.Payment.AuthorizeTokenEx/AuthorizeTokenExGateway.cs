using AuthorizeNet;
using EPiServer.Business.Commerce.Plugins.Payment.Authorize;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Payments.Tokenization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Plugins.Payment.Authorize;
using System;
using System.Linq;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx
{
    /// <summary>
    /// Authorizes and processes <see cref="ITokenizedPayment"/>.
    /// </summary>
    public class AuthorizeTokenExGateway : AuthorizePaymentGateway
    {
        public AuthorizeTokenExGateway()
        {
        }

        /// <summary>
        /// Processes the payment.
        /// </summary>
        /// <param name="orderGroup">The order group.</param>
        /// <param name="payment">The payment.</param>
        public override PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            if (orderGroup is null)
            {
                throw new ArgumentException("Order group should not be null.");
            }

            if (!(payment is ITokenizedPayment tokenPayment))
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "Payment is not tokenization.");
            }

            var message = string.Empty;
            var transactionRequest = new TransactionRequest(
                GetSetting(UserParameterName),
                GetSetting(TransactionKeyParameterName),
                GetTestModeSetting());
            var authorizeNetService = new AuthorizeTokenExService(transactionRequest, new TokenExGateway());

            //Process purchase order
            if (IsRegularTransaction(orderGroup))
            {
                var address = tokenPayment.BillingAddress;
                if (address == null)
                {
                    throw new PaymentException(PaymentException.ErrorType.ConfigurationError, "", "Billing address was not specified.");
                }

                var transactionData = CreateTransactionDataForRegularTransaction(orderGroup, tokenPayment);
                var merchantCurrency = authorizeNetService.GetMerchantCurrency();
                var amount = Utilities.ConvertMoney(new Money(tokenPayment.Amount, orderGroup.Currency), merchantCurrency);

                var manager = new AuthorizeTokenExCimBasedManager(
                    authorizeNetService,
                    ServiceLocator.Current.GetInstance<IOrderGroupCalculator>(),
                    ServiceLocator.Current.GetInstance<IShippingCalculator>(),
                    ServiceLocator.Current.GetInstance<ILineItemCalculator>(),
                    ServiceLocator.Current.GetInstance<IMarketService>());
                var response = manager.Process(orderGroup, transactionRequest, transactionData, amount, tokenPayment);

                if (response.IsTransactionCapturedOrSettled)
                {
                    PostProcessPayment(tokenPayment);
                    return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                }

                var result = (GatewayResponse)response.Response;
                if (result.Error || result.Declined)
                {
                    var exception = new PaymentException(PaymentException.ErrorType.ProviderError, result.ResponseCode, result.Message);
                    exception.ResponseMessages.Add("CSCResponse", result.CAVResponse);
                    exception.ResponseMessages.Add("ReasonCode", result.ResponseReasonCode);
                    exception.ResponseMessages.Add("Subcode", $"{result.SubCode}");
                    exception.ResponseMessages.Add("AVSResult", result.AVSResponse);

                    throw exception;
                }

                tokenPayment.AuthorizationCode = tokenPayment.ValidationCode = response.Response.AuthorizationCode;
                if (transactionData.type == TransactionType.Authorization)
                {
                    tokenPayment.TransactionID = response.Response.TransactionID;
                }

                // When doing capture, refund, etc... transactions, Authorize.Net will return a new Transaction Id. We need to store this to ProviderTransactionID
                // instead of TransactionID, because TransactionID should be the Authorization transaction Id, and ProviderTransactionID will be used when we want to refund.
                tokenPayment.ProviderTransactionID = response.Response.TransactionID;

                message = response.Response.Message;
            }

            PostProcessPayment(tokenPayment);

            //TODO Supports Payment plan
            return PaymentProcessingResult.CreateSuccessfulResult(message);
        }

        /// <summary>
        /// Post process payment, remove token form payment.
        /// </summary>
        /// <param name="tokenPayment">Input payment, it is supposed to be a ITokenizationPayment.</param>
        private void PostProcessPayment(ITokenizedPayment tokenPayment)
        {
            tokenPayment.Token = string.Empty;
        }

        private TransactionData CreateTransactionDataForRegularTransaction(
            IOrderGroup orderGroup,
            ITokenizedPayment tokenPayment)
        {
            var transData = new TransactionData
            {
                AuthorizationCode = tokenPayment.ValidationCode,
            };

            if (string.IsNullOrEmpty(tokenPayment.TransactionType) || !Enum.TryParse(tokenPayment.TransactionType, out transData.type))
            {
                var type = Settings[PaymentOptionParameterName].Equals(TransactionType.Sale.ToString(), StringComparison.OrdinalIgnoreCase) ? TransactionType.Sale : TransactionType.Authorization;
                if (string.IsNullOrEmpty(tokenPayment.TransactionType))
                {
                    tokenPayment.TransactionType = type.ToString();
                }

                transData.type = type;
            }

            var orderForm = orderGroup.Forms.FirstOrDefault(form => form.Payments.Contains(tokenPayment));

            if (transData.type == TransactionType.Capture &&
                orderForm != null && orderForm.CapturedPaymentTotal != 0 &&
                orderForm.Payments.Count(p => p.PaymentMethodId == tokenPayment.PaymentMethodId &&
                                              p.TransactionType == TransactionType.Capture.ToString()) > 1)
            {
                // from the second capture, the transaction type must be CAPTURE_ONLY instead of PRIOR_AUTH_CAPTURE
                transData.type = TransactionType.CaptureOnly;
            }

            if (transData.type != TransactionType.Authorization)
            {
                // ProviderTransactionID might be null in site-upgrade case. Then we should get TransactionID in that case.
                transData.transId = !string.IsNullOrEmpty(tokenPayment.ProviderTransactionID) ? tokenPayment.ProviderTransactionID : tokenPayment.TransactionID;
            }

            if (orderGroup is IPurchaseOrder purchaseOrder)
            {
                transData.purchaseOrderNum = purchaseOrder.OrderNumber;

                var paymentIndex = orderGroup.Forms.SelectMany(form => form.Payments).Count();
                transData.invoiceNum = $"{transData.purchaseOrderNum}-{paymentIndex}";
            }
            else if (orderGroup is ICart)
            {
                string orderNumber;
                if (orderGroup is Cart cart)
                {
                    orderNumber = cart.GenerateOrderNumber(cart);
                    cart.OrderNumberMethod = c => orderNumber;
                    cart.AcceptChanges();
                }
                else
                {
                    var orderNumberGenerator = ServiceLocator.Current.GetInstance<IOrderNumberGenerator>();
                    orderNumber = orderNumberGenerator.GenerateOrderNumber(orderGroup);
                    orderGroup.Properties["OrderNumber"] = orderNumber;
                }

                transData.invoiceNum = orderNumber;
            }

            return transData;
        }
    }
}