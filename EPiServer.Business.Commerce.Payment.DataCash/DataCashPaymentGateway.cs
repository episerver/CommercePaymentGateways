using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Core.Features;
using Mediachase.Commerce.Extensions;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Plugins.Payment;
using Mediachase.Commerce.Security;
using Mediachase.Data.Provider;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    public class DataCashPaymentGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IFeatureSwitch _featureSwitch;
        private readonly IInventoryProcessor _inventoryProcessor;
        private readonly IOrderNumberGenerator _orderNumberGenerator;
        private readonly LocalizationService _localizationService;
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(DataCashPaymentGateway));

        private DataCashConfiguration _dataCashConfiguration;
        private RequestDocumentCreation _requestDocumentCreation;

        public const string DataCashReferencePropertyName = "DataCashReference";
        public const string DataCashAuthenticateCodePropertyName = "DataCashAuthenticateCode";
        public const string DataCashMerchantReferencePropertyName = "DataCashMerchantReference";
        
        /// <summary>
        /// Gets or sets the order group containing processing payment.
        /// </summary>
        public IOrderGroup OrderGroup { get; set; }
                
        public DataCashPaymentGateway() : this(
            ServiceLocator.Current.GetInstance<IOrderRepository>(),
            ServiceLocator.Current.GetInstance<IInventoryProcessor>(),
            ServiceLocator.Current.GetInstance<IOrderNumberGenerator>(),
            ServiceLocator.Current.GetInstance<IFeatureSwitch>(),
            ServiceLocator.Current.GetInstance<LocalizationService>(),
            new DataCashConfiguration(),
            new RequestDocumentCreation())
        {
        }

        public DataCashPaymentGateway(
            IOrderRepository orderRepository,
            IInventoryProcessor inventoryProcessor,
            IOrderNumberGenerator orderNumberGenerator,
            IFeatureSwitch featureSwitch,
            LocalizationService localizationService,
            DataCashConfiguration dataCashConfiguration,
            RequestDocumentCreation requestDocumentCreation)
        {
            _orderRepository = orderRepository;
            _inventoryProcessor = inventoryProcessor;
            _orderNumberGenerator = orderNumberGenerator;
            _localizationService = localizationService;
            _featureSwitch = featureSwitch;

            _dataCashConfiguration = dataCashConfiguration;
            _requestDocumentCreation = requestDocumentCreation;
        }

        /// <summary>
        /// Processes the payment.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <param name="message">The message.</param>
        /// <remarks>When "Complete" or "Refund" shipment in Commerce Manager, this method will be called again with the TransactionType is Capture/Credit</remarks>
        /// <returns><c>True</c> if payment processed successfully, otherwise <c>False</c></returns>
        public override bool ProcessPayment(Mediachase.Commerce.Orders.Payment payment, ref string message)
        {
            var orderGroup = payment.Parent.Parent;

            var paymentProcessingResult = ProcessPayment(orderGroup, payment);

            if (!string.IsNullOrEmpty(paymentProcessingResult.RedirectUrl))
            {
                HttpContext.Current.Response.Redirect(paymentProcessingResult.RedirectUrl);
            }

            message = paymentProcessingResult.Message;
            return paymentProcessingResult.IsSuccessful;
        }

        /// <summary>
        /// Processes the payment.
        /// </summary>
        /// <param name="orderGroup">The order group.</param>
        /// <param name="payment">The payment.</param>
        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            if (HttpContext.Current == null)
            {
                return PaymentProcessingResult.CreateSuccessfulResult(Utilities.Translate("ProcessPaymentNullHttpContext"));
            }

            if (payment == null)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(Utilities.Translate("PaymentNotSpecified"));
            }

            var orderForm = orderGroup.Forms.FirstOrDefault(f => f.Payments.Contains(payment));
            if (orderForm == null)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(Utilities.Translate("PaymentNotAssociatedOrderForm"));
            }

            var purchaseOrder = orderGroup as IPurchaseOrder;
            if (purchaseOrder != null)
            {
                // When a refund is created by return process, 
                // this method will be called again with the TransactionType is Credit
                if (payment.TransactionType.Equals(TransactionType.Credit.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // process refund
                    // Using Transaction id as DataCash Reference to handle upgrade case.  Assume that before upgrading to this version, our system has some authorized transactions that need to be captured.
                    // After upgrading, using Provider Transaction id instead.
                    return SendRefundRequest(purchaseOrder, payment);
                }

                // active invoice when order is complete
                // when user click complete order in commerce manager the transaction type will be Capture
                if (payment.TransactionType.Equals(TransactionType.Capture.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var result = SendFulfillRequest(purchaseOrder, orderForm, payment);
                    var message = result.Message;

                    if (!result.IsSuccessful && !string.IsNullOrEmpty(result.Message))
                    {
                        _logger.Error(message);
                        message = $"{Utilities.Translate("GenericError")}:{message}";
                    }

                    return result;
                }
            }

            return ProcessPaymentCheckout(payment, (ICart)orderGroup);
        }

        /// <summary>
        /// Processes the successful transaction, was called when redirect back from DataCash.
        /// </summary>
        /// <param name="orderGroup">The order group that was processed.</param>
        /// <param name="payment">The order payment.</param>
        /// <param name="acceptUrl">The redirect url when finished.</param>
        /// <param name="cancelUrl">The redirect url when error happens.</param>
        /// <returns>The url redirection after process.</returns>
        public string ProcessSuccessfulTransaction(IOrderGroup orderGroup, IPayment payment, string acceptUrl, string cancelUrl)
        {
            var cart = orderGroup as ICart;
            if (cart == null)
            {
                // return to the shopping cart page immediately and show error messages
                return ProcessUnsuccessfulTransaction(cancelUrl, Utilities.Translate("CommitTranErrorCartNull"));
            }

            using (var scope = new TransactionScope())
            {
                var orderForm = orderGroup.Forms.FirstOrDefault(f => f.Payments.Contains(payment));
                string authenticateCode;
                var result = PreAuthenticateRequest(orderGroup, orderForm, payment, out authenticateCode);

                if (!result.IsSuccessful && string.IsNullOrEmpty(authenticateCode))
                {
                    _logger.Error(result.Message);
                    return ProcessUnsuccessfulTransaction(cancelUrl, result.Message);
                }

                var errorMessages = new List<string>();
                var cartCompleted = DoCompletingCart(cart, errorMessages);

                if (!cartCompleted)
                {
                    return UriSupport.AddQueryString(cancelUrl, "message", string.Join(";", errorMessages.Distinct().ToArray()));
                }

                payment.Properties[DataCashAuthenticateCodePropertyName] = authenticateCode;
                payment.TransactionID = payment.Properties[DataCashReferencePropertyName] as string;

                // Save changes
                var orderReference = _orderRepository.SaveAsPurchaseOrder(orderGroup);
                var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);
                purchaseOrder.OrderNumber = _orderNumberGenerator.GenerateOrderNumber(purchaseOrder);

                _orderRepository.Save(purchaseOrder);
                _orderRepository.Delete(orderGroup.OrderLink);

                scope.Complete();

                var email = payment.BillingAddress.Email;

                acceptUrl = UriSupport.AddQueryString(acceptUrl, "success", "true");
                acceptUrl = UriSupport.AddQueryString(acceptUrl, "contactId", purchaseOrder.CustomerId.ToString());
                acceptUrl = UriSupport.AddQueryString(acceptUrl, "orderNumber", purchaseOrder.OrderLink.OrderGroupId.ToString());
                acceptUrl = UriSupport.AddQueryString(acceptUrl, "notificationMessage", string.Format(_localizationService.GetString("/OrderConfirmationMail/ErrorMessages/SmtpFailure"), email));
                acceptUrl = UriSupport.AddQueryString(acceptUrl, "email", email);
            }
            return acceptUrl;
        }

        /// <summary>
        /// Processes the unsuccessful transaction.
        /// </summary>
        /// <param name="cancelUrl">The cancel url.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The url redirection after process.</returns>
        public string ProcessUnsuccessfulTransaction(string cancelUrl, string errorMessage)
        {
            if (HttpContext.Current == null)
            {
                return cancelUrl;
            }

            _logger.Error($"DataCash transaction failed [{errorMessage}].");
            return UriSupport.AddQueryString(cancelUrl, "message", errorMessage);
        }

        /// <summary>
        /// Validates and completes a cart.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="errorMessages">The error messages.</param>
        private bool DoCompletingCart(ICart cart, IList<string> errorMessages)
        {
            foreach (var p in cart.Forms.SelectMany(f => f.Payments).Where(p => p != null))
            {
                PaymentStatusManager.ProcessPayment(p);
            }

            var isSuccess = true;

            if (_featureSwitch.IsSerializedCartsEnabled())
            {
                var validationIssues = new Dictionary<ILineItem, IList<ValidationIssue>>();
                cart.AdjustInventoryOrRemoveLineItems((item, issue) => AddValidationIssues(validationIssues, item, issue), _inventoryProcessor);

                isSuccess = !validationIssues.Any();

                foreach (var issue in validationIssues.Values.SelectMany(x => x).Distinct())
                {
                    if (issue == ValidationIssue.RejectedInventoryRequestDueToInsufficientQuantity)
                    {
                        errorMessages.Add(Utilities.Translate("NotEnoughStockWarning"));
                    }
                    else
                    {
                        errorMessages.Add(Utilities.Translate("CartValidationWarning"));
                    }
                }

                return isSuccess;
            }

            // Execute CheckOutWorkflow with parameter to ignore running process payment activity again.
            var isIgnoreProcessPayment = new Dictionary<string, object> { { "PreventProcessPayment", true } };
            var workflowResults = OrderGroupWorkflowManager.RunWorkflow((OrderGroup)cart, OrderGroupWorkflowManager.CartCheckOutWorkflowName, true, isIgnoreProcessPayment);

            var warnings = workflowResults.OutputParameters["Warnings"] as StringDictionary;
            isSuccess = warnings.Count == 0;

            foreach (string message in warnings.Values)
            {
                errorMessages.Add(message);
            }

            return isSuccess;
        }

        private void AddValidationIssues(IDictionary<ILineItem, IList<ValidationIssue>> issues, ILineItem lineItem, ValidationIssue issue)
        {
            if (!issues.ContainsKey(lineItem))
            {
                issues.Add(lineItem, new List<ValidationIssue>());
            }

            if (!issues[lineItem].Contains(issue))
            {
                issues[lineItem].Add(issue);
            }
        }

        private PaymentProcessingResult ProcessPaymentCheckout(IPayment payment, ICart cart)
        {
            var merchRef = DateTime.Now.Ticks.ToString();
            payment.Properties[DataCashMerchantReferencePropertyName] = merchRef; // A unique reference number for each transaction (Min 6, max 30 alphanumeric character)

            var notifyUrl = UriSupport.AbsoluteUrlBySettings(Utilities.GetUrlFromStartPageReferenceProperty("DataCashPaymentPage"));
            notifyUrl = UriSupport.AddQueryString(notifyUrl, "accept", "true");
            notifyUrl = UriSupport.AddQueryString(notifyUrl, "hash", Utilities.GetMD5Key(merchRef + "accepted"));

            var requestDoc = _requestDocumentCreation.CreateDocumentForPaymentCheckout(cart, payment, notifyUrl);

            var responseDoc = DocumentHelpers.SendTransaction(requestDoc, _dataCashConfiguration.Config);
            string redirectUrl;
            if (DocumentHelpers.IsSuccessful(responseDoc))
            {
                redirectUrl = $"{responseDoc.get("Response.HpsTxn.hps_url")}?HPS_SessionID={responseDoc.get("Response.HpsTxn.session_id")}";
                payment.Properties[DataCashReferencePropertyName] = responseDoc.get("Response.datacash_reference");
            }
            else
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(DocumentHelpers.GetErrorMessage(responseDoc));
            }

            _orderRepository.Save(cart);

            var message = $"---DataCash--. Redirect end user to {redirectUrl}";
            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message, redirectUrl);
        }

        private PaymentProcessingResult PreAuthenticateRequest(IOrderGroup orderGroup, IOrderForm orderForm, IPayment payment, out string authenticateCode)
        {
            authenticateCode = string.Empty;
            try
            {
                var requestDoc = _requestDocumentCreation.CreateDocumentForPreAuthenticateRequest(payment, orderForm, orderGroup.Currency);
                var responseDoc = DocumentHelpers.SendTransaction(requestDoc, _dataCashConfiguration.Config);
                if (DocumentHelpers.IsSuccessful(responseDoc))
                {
                    authenticateCode = responseDoc.get("Response.CardTxn.authcode");
                    return string.IsNullOrEmpty(authenticateCode) ?
                        PaymentProcessingResult.CreateUnsuccessfulResult(string.Empty) :
                        PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                }

                return PaymentProcessingResult.CreateUnsuccessfulResult(DocumentHelpers.GetErrorMessage(responseDoc));
            }
            catch (System.Exception e)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(e.Message);
            }
        }

        private PaymentProcessingResult SendFulfillRequest(IPurchaseOrder po, IOrderForm orderForm, IPayment payment)
        {
            try
            {
                var requestDoc = _requestDocumentCreation.CreateDocumentForFulfillRequest(payment, orderForm);
                var responseDoc = DocumentHelpers.SendTransaction(requestDoc, _dataCashConfiguration.Config);
                if (DocumentHelpers.IsSuccessful(responseDoc))
                {
                    // Extract the response details.
                    // When doing capture, refund, etc... transactions, DataCase will return a new Reference Id. We need to store this to ProviderTransactionID
                    // instead of TransactionID, because TransactionID should be the Authorization reference Id, and ProviderTransactionID will be used when we want to refund.
                    payment.ProviderTransactionID = DocumentHelpers.GetResponseInfo(responseDoc, "Response.datacash_reference");

                    var message = string.Format("[{0}] [Capture payment-{1}] [Status: {2}] .Response: {3} at Time stamp={4}",
                        payment.PaymentMethodName,
                        DocumentHelpers.GetResponseInfo(responseDoc, "Response.merchantreference"),
                        DocumentHelpers.GetResponseInfo(responseDoc, "Response.status"),
                        DocumentHelpers.GetResponseInfo(responseDoc, "Response.reason"),
                        DocumentHelpers.GetResponseInfo(responseDoc, "Response.time")
                    );

                    // add a new order note about this capture
                    AddNoteToPurchaseOrder(po, po.CustomerId, "CAPTURE", message);
                    _orderRepository.Save(po);

                    return PaymentProcessingResult.CreateSuccessfulResult(message);
                }

                return PaymentProcessingResult.CreateUnsuccessfulResult(DocumentHelpers.GetErrorMessage(responseDoc));
            }
            catch (System.Exception e)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(e.Message);
            }
        }

        private PaymentProcessingResult SendRefundRequest(IPurchaseOrder po, IPayment payment)
        {
            try
            {
                var requestDoc = _requestDocumentCreation.CreateDocumentForRefundRequest(payment);
                var responseDoc = DocumentHelpers.SendTransaction(requestDoc, _dataCashConfiguration.Config);
                if (DocumentHelpers.IsSuccessful(responseDoc))
                {
                    payment.ProviderTransactionID = DocumentHelpers.GetResponseInfo(responseDoc, "Response.datacash_reference");

                    var message = string.Format("[{0}] [RefundTransaction-{1}] [Status: {2}] Response: {3} at Time stamp={4}.",
                        payment.PaymentMethodName,
                        DocumentHelpers.GetResponseInfo(responseDoc, "Response.datacash_reference"),
                        DocumentHelpers.GetResponseInfo(responseDoc, "Response.status"),
                        DocumentHelpers.GetResponseInfo(responseDoc, "Response.reason"),
                        DocumentHelpers.GetResponseInfo(responseDoc, "Response.time")
                    );

                    // add a new order note about this refund
                    AddNoteToPurchaseOrder(po, po.CustomerId, "REFUND", message);
                    _orderRepository.Save(po);

                    return PaymentProcessingResult.CreateSuccessfulResult(message);
                }

                return PaymentProcessingResult.CreateUnsuccessfulResult(DocumentHelpers.GetErrorMessage(responseDoc));
            }
            catch (System.Exception e)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(e.Message);
            }
        }

        private void AddNoteToPurchaseOrder(IPurchaseOrder purchaseOrder, Guid customerId, string title, string detail)
        {
            var orderNote = purchaseOrder.CreateOrderNote();
            orderNote.Type = OrderNoteTypes.System.ToString();
            orderNote.CustomerId = customerId != Guid.Empty ? customerId : PrincipalInfo.CurrentPrincipal.GetContactId();
            orderNote.Title = title;
            orderNote.Detail = detail;
            orderNote.Created = DateTime.UtcNow;
            purchaseOrder.Notes.Add(orderNote);
        }
    }
}