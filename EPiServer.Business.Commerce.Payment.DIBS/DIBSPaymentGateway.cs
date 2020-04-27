using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Mediachase.Commerce.Core.Features;
using Mediachase.Commerce.Customers;
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
using EPiServer.Data;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    public class DIBSPaymentGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        private readonly IFeatureSwitch _featureSwitch;
        private readonly IInventoryProcessor _inventoryProcessor;
        private readonly IOrderRepository _orderRepository;
        private readonly LocalizationService _localizationService;
        private readonly DIBSRequestHelper _dibsRequestHelper;
        private static readonly Lazy<DatabaseMode> _databaseMode = new Lazy<DatabaseMode>(GetDefaultDatabaseMode);

        public DIBSPaymentGateway()
            : this(
            ServiceLocator.Current.GetInstance<IFeatureSwitch>(),
            ServiceLocator.Current.GetInstance<IInventoryProcessor>(),
            ServiceLocator.Current.GetInstance<IOrderRepository>(),
            ServiceLocator.Current.GetInstance<LocalizationService>())
        {
        }

        public DIBSPaymentGateway(
            IFeatureSwitch featureSwitch,
            IInventoryProcessor inventoryProcessor,
            IOrderRepository orderRepository,
            LocalizationService localizationService)
        {
            _featureSwitch = featureSwitch;
            _inventoryProcessor = inventoryProcessor;
            _orderRepository = orderRepository;
            _localizationService = localizationService;

            _dibsRequestHelper = new DIBSRequestHelper();
        }

        /// <summary>
        /// Main entry point of ECF Payment Gateway.
        /// </summary>
        /// <param name="payment">The payment to process</param>
        /// <param name="message">The message.</param>
        /// <returns>return false and set the message will make the WorkFlow activity raise PaymentExcetion(message)</returns>
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
                if (payment.TransactionType == TransactionType.Capture.ToString())
                {
                    // return true meaning the capture request is done,
                    // actual capturing must be done on DIBS.
                    var result = _dibsRequestHelper.PostCaptureRequest(payment, purchaseOrder);
                    var status = result["status"];
                    if (status == "ACCEPT")
                    {
                        return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                    }

                    return PaymentProcessingResult.CreateUnsuccessfulResult(
                        $@"There was an error while capturing payment with DIBS.
                        status: {status}
                        declineReason: {result["declineReason"]}");
                }

                if (payment.TransactionType == TransactionType.Credit.ToString())
                {
                    var transactionID = payment.TransactionID;
                    if (string.IsNullOrEmpty(transactionID) || transactionID.Equals("0"))
                    {
                        return PaymentProcessingResult.CreateUnsuccessfulResult("TransactionID is not valid or the current payment method does not support this order type.");
                    }
                    // The transact must be captured before refunding
                    var result = _dibsRequestHelper.PostRefundRequest(payment, purchaseOrder);
                    var status = result["status"];
                    if (status == "ACCEPT")
                    {
                        return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                    }

                    return PaymentProcessingResult.CreateUnsuccessfulResult(
                        $@"There was an error while capturing payment with DIBS.
                        status: {status}
                        declineReason: {result["declineReason"]}");
                }

                // right now we do not support processing the order which is created by Commerce Manager
                return PaymentProcessingResult.CreateUnsuccessfulResult("The current payment method does not support this order type.");
            }

            var cart = orderGroup as ICart;
            if (cart != null && cart.OrderStatus == OrderStatus.Completed)
            {
                return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
            }
            _orderRepository.Save(orderGroup);

            var redirectUrl = Utilities.GetUrlFromStartPageReferenceProperty("DIBSPaymentPage");

            return PaymentProcessingResult.CreateSuccessfulResult(string.Empty, redirectUrl);
        }

        /// <summary>
        /// Processes the unsuccessful transaction.
        /// </summary>
        /// <param name="cancelUrl">The cancel url.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The url redirection after process.</returns>
        public string ProcessUnsuccessfulTransaction(string cancelUrl, string errorMessage)
        {
            return UriUtil.AddQueryString(cancelUrl, "message", errorMessage);
        }

        /// <summary>
        /// Processes the successful transaction, will be called when DIBS server processes 
        /// the payment successfully and redirect back.
        /// </summary>
        /// <param name="cart">The cart that was processed.</param>
        /// <param name="payment">The order payment.</param>
        /// <param name="transactionID">The transaction id.</param>
        /// <param name="orderNumber">The order number.</param>
        /// <param name="acceptUrl">The redirect url when finished.</param>
        /// <param name="cancelUrl">The redirect url when error happens.</param>
        /// <returns>The redirection url after processing.</returns>
        public string ProcessSuccessfulTransaction(ICart cart, IPayment payment, string transactionID, string orderNumber, string acceptUrl, string cancelUrl)
        {
            if (cart == null)
            {
                return cancelUrl;
            }

            string redirectionUrl;

            // Change status of payments to processed.
            // It must be done before execute workflow to ensure payments which should mark as processed.
            // To avoid get errors when executed workflow.
            PaymentStatusManager.ProcessPayment(payment);

            var errorMessages = new List<string>();
            var cartCompleted = DoCompletingCart(cart, errorMessages);

            if (!cartCompleted)
            {
                return UriUtil.AddQueryString(cancelUrl, "message", string.Join(";", errorMessages.Distinct().ToArray()));
            }

            // Save the transact from DIBS to payment.
            payment.TransactionID = transactionID;

            var purchaseOrder = MakePurchaseOrder(cart, orderNumber);

            redirectionUrl = UpdateAcceptUrl(purchaseOrder, payment, acceptUrl);

            return redirectionUrl;
        }

        private IPurchaseOrder MakePurchaseOrder(ICart cart, string orderNumber)
        {
            // Save changes
            //this might cause problem when checkout using multiple shipping address because ECF workflow does not handle it. Modify the workflow instead of modify in this payment
            var purchaseOrderLink = _orderRepository.SaveAsPurchaseOrder(cart);
            var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(purchaseOrderLink.OrderGroupId);

            UpdateOrder(purchaseOrder, orderNumber);

            if (_databaseMode.Value != DatabaseMode.ReadOnly)
            {
                UpdateLastOrderOfCurrentContact(CustomerContext.Current.CurrentContact, purchaseOrder.Created);
            }

            AddNoteToPurchaseOrder($"New order placed by {PrincipalInfo.CurrentPrincipal.Identity.Name} in Public site", purchaseOrder);

            _orderRepository.Save(purchaseOrder);

            // Remove old cart
            _orderRepository.Delete(cart.OrderLink);

            return purchaseOrder;
        }

        private void UpdateOrder(IPurchaseOrder purchaseOrder, string orderNumber)
        {
            purchaseOrder.OrderStatus = OrderStatus.InProgress;
            purchaseOrder.OrderNumber = orderNumber;

            // Update display name of product by current language
            Utilities.UpdateDisplayNameWithCurrentLanguage(purchaseOrder);
        }

        /// <summary>
        /// Update last order time stamp which current user completed.
        /// </summary>
        /// <param name="contact">The customer contact.</param>
        /// <param name="datetime">The order time.</param>
        private void UpdateLastOrderOfCurrentContact(CustomerContact contact, DateTime datetime)
        {
            if (contact != null)
            {
                contact.LastOrder = datetime;
                contact.SaveChanges();
            }
        }

        private string UpdateAcceptUrl(IPurchaseOrder purchaseOrder, IPayment payment, string acceptUrl)
        {
            var redirectionUrl = UriUtil.AddQueryString(acceptUrl, "success", "true");
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "contactId", purchaseOrder.CustomerId.ToString());
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "orderNumber", purchaseOrder.OrderLink.OrderGroupId.ToString());
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "notificationMessage", string.Format(_localizationService.GetString("/OrderConfirmationMail/ErrorMessages/SmtpFailure"), payment.BillingAddress.Email));
            redirectionUrl = UriUtil.AddQueryString(redirectionUrl, "email", payment.BillingAddress.Email);
            return redirectionUrl;
        }

        /// <summary>
        /// Validates and completes a cart.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="errorMessages">The error messages.</param>
        private bool DoCompletingCart(ICart cart, IList<string> errorMessages)
        {
            var isSuccess = true;

            if (_databaseMode.Value != DatabaseMode.ReadOnly)
            {
                if (_featureSwitch.IsSerializedCartsEnabled())
                {
                    var validationIssues = new Dictionary<ILineItem, IList<ValidationIssue>>();
                    cart.AdjustInventoryOrRemoveLineItems(
                        (item, issue) => AddValidationIssues(validationIssues, item, issue), _inventoryProcessor);

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
                var isIgnoreProcessPayment = new Dictionary<string, object> {{"PreventProcessPayment", true}};
                var workflowResults = OrderGroupWorkflowManager.RunWorkflow((OrderGroup) cart,
                    OrderGroupWorkflowManager.CartCheckOutWorkflowName, true, isIgnoreProcessPayment);

                var warnings = workflowResults.OutputParameters["Warnings"] as StringDictionary;
                isSuccess = warnings.Count == 0;

                foreach (string message in warnings.Values)
                {
                    errorMessages.Add(message);
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// Adds the note to purchase order.
        /// </summary>
        /// <param name="note">The note detail.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        private void AddNoteToPurchaseOrder(string note, IPurchaseOrder purchaseOrder)
        {
            var orderNote = purchaseOrder.CreateOrderNote();
            orderNote.Type = OrderNoteTypes.System.ToString();
            orderNote.CustomerId = PrincipalInfo.CurrentPrincipal.GetContactId();
            orderNote.Title = note.Substring(0, Math.Min(note.Length, 24)) + "...";
            orderNote.Detail = note;
            orderNote.Created = DateTime.UtcNow;
            purchaseOrder.Notes.Add(orderNote);
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

        private static DatabaseMode GetDefaultDatabaseMode()
        {
            if (!_databaseMode.IsValueCreated)
            {
                return ServiceLocator.Current.GetInstance<IDatabaseMode>().DatabaseMode;
            }
            return _databaseMode.Value;
        }
    }
}