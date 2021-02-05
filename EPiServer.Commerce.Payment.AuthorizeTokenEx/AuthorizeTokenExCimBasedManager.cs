using AuthorizeNet;
using AuthorizeNet.Api.Contracts.V1;
using EPiServer.Business.Commerce.Plugins.Payment.Authorize;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Payments.Tokenization;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Plugins.Payment.Authorize;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx
{
    /// <summary>
    /// Contains all the functions needed to perform operations for <see cref="AuthorizeTokenExGateway"/>.
    /// </summary>
    public class AuthorizeTokenExCimBasedManager
    {
        // The authorize.net allow decimal value up to four decimal places.
        private const int DecimalDigit = 4;
        private readonly AuthorizeTokenExService _authorizeTokenExService;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IShippingCalculator _shippingCalculator;
        private readonly ILineItemCalculator _lineItemCalculator;
        private readonly IMarketService _marketService;

        public AuthorizeTokenExCimBasedManager(
            AuthorizeTokenExService authorizeNetService,
            IOrderGroupCalculator orderGroupCalculator,
            IShippingCalculator shippingCalculator,
            ILineItemCalculator lineItemCalculator,
            IMarketService marketService)
        {
            _authorizeTokenExService = authorizeNetService;
            _orderGroupCalculator = orderGroupCalculator;
            _shippingCalculator = shippingCalculator;
            _lineItemCalculator = lineItemCalculator;
            _marketService = marketService;
        }

        public TransactionResponse Process(
            IOrderGroup orderGroup,
            TransactionRequest transactionRequest,
            TransactionData transactionData,
            decimal amount,
            ITokenizedPayment payment)
        {
            switch (transactionData.type)
            {
                case TransactionType.Authorization:
                    return ProcessAuthorizeRequest(orderGroup, transactionData, payment, amount);
                case TransactionType.Capture:
                    return ProcessPriorCaptureRequest(transactionRequest, transactionData, amount);
                case TransactionType.Sale:
                    return ProcessSaleRequest(orderGroup, transactionData, payment, amount);
                case TransactionType.Credit:
                    return ProcessCreditRequest(transactionRequest, transactionData, payment, amount);
                case TransactionType.Void:
                    return ProcessVoidRequest(transactionData, payment);
                case TransactionType.CaptureOnly:
                    return ProcessCaptureOnlyRequest(transactionRequest, transactionData, payment, amount);
                default:
                    throw new NotImplementedException("Not implemented TransactionType: " + transactionData.type);
            }
        }

        public virtual bool IsTransactionCapturedOrSettled(TransactionData transData, TransactionRequest transactionRequest)
        {
            //Transaction Captured: when is already captured on the Authorized.Net web page: Authorization Amount is always equal to settlement amount
            //when is captured via api in multishipments: First shipment will make the capture and update the settlement amount to the amount of the shipment
            //doing partial capture, for successive shipments settle amount will be minor than Authorization amount so that is not considered as already captured.
            //Or Transaction Settled: When the transaction status is settledSuccessfully.
            var transaction = GetTransactionDetails(transData, transactionRequest);

            if (transaction.transactionStatus == AuthorizeTokenExGateway.SettledStatus)
            {
                return true;
            }

            return transaction.transactionStatus == AuthorizeTokenExGateway.CapturedPendingSettlementStatus && transaction.settleAmount == transaction.authAmount;
        }

        public virtual transactionDetailsType GetTransactionDetails(TransactionData transData, TransactionRequest transactionRequest)
        {
            var request = new getTransactionDetailsRequest
            {
                transId = transData.transId,
                merchantAuthentication = _authorizeTokenExService.GetMerchantAuthenticationType()
            };

            var transactionDetails = _authorizeTokenExService.GetTransactionDetails(request);
            return transactionDetails == null
                ? throw new PaymentException(PaymentException.ErrorType.ProviderError, "", "E00011: Access denied. You do not have permission to call the Transaction Details API.")
                : transactionDetails;
        }

        public virtual string AddCreditCard(IOrderGroup orderGroup, string customerProfileId, string cardNumber, int expMonth, int expYear)
        {
            try
            {
                return _authorizeTokenExService.AddCreditCard(customerProfileId, cardNumber, expMonth, expYear, null, null);
            }
            catch (System.Exception ex)
            {
                if (!ex.Message.Contains("E00039"))
                {
                    throw new PaymentException(PaymentException.ErrorType.ProviderError, string.Empty, ex.Message, ex);
                }

                var processedPayments = orderGroup.Forms.SelectMany(form => form.Payments.Where(p => !string.IsNullOrEmpty(p.TransactionID)));

                throw new PaymentException(PaymentException.ErrorType.ProviderError, string.Empty, ex.Message, ex);
            }
        }

        public virtual string AddShippingAddress(string customerProfileId, AuthorizeNet.Address address)
        {
            try
            {
                return _authorizeTokenExService.AddShippingAddress(customerProfileId, address);
            }
            catch (System.Exception ex)
            {
                if (!ex.Message.Contains("E00039"))
                {
                    throw new PaymentException(PaymentException.ErrorType.ProviderError, string.Empty, ex.Message, ex);
                }

                var customer = _authorizeTokenExService.GetCustomer(customerProfileId);
                if (customer != null)
                {
                    var shippingAddress = customer.ShippingAddresses.FirstOrDefault(sa =>
                    sa.First == address.First &&
                    sa.Last == address.Last &&
                    sa.City == address.City &&
                    sa.Country == address.Country &&
                    sa.Fax == address.Fax &&
                    sa.Phone == address.Phone &&
                    sa.State == address.State &&
                    sa.Street == address.Street &&
                    sa.Zip == address.Zip);

                    return shippingAddress?.ID;
                }

                throw new PaymentException(PaymentException.ErrorType.ProviderError, string.Empty, ex.Message, ex);
            }
        }

        private TransactionResponse ProcessAuthorizeRequest(IOrderGroup orderGroup, TransactionData transactionData, ITokenizedPayment payment, decimal amount)
        {
            var order = CreatePaymentOrder(orderGroup, transactionData, payment, amount);

            return new TransactionResponse(_authorizeTokenExService.ProcessAuthorizeRequest(order), false);
        }

        private TransactionResponse ProcessSaleRequest(IOrderGroup orderGroup, TransactionData transactionData, ITokenizedPayment payment, decimal amount)
        {
            var order = CreatePaymentOrder(orderGroup, transactionData, payment, amount);

            return new TransactionResponse(_authorizeTokenExService.AuthorizeAndCapture(order), false);
        }

        private TransactionResponse ProcessVoidRequest(TransactionData transData, ITokenizedPayment payment) =>
            new TransactionResponse(
                _authorizeTokenExService.ProcessVoidRequest(
                    payment.Properties[AuthorizeTokenExGateway.ProviderProfileIdPropertyName] as string,
                    string.Empty,
                    transData.transId),
                false);

        private TransactionResponse ProcessPriorCaptureRequest(TransactionRequest transactionRequest, TransactionData transData, decimal amount) =>
            IsTransactionCapturedOrSettled(transData, transactionRequest) ?
                new TransactionResponse(null, true) :
                new TransactionResponse(_authorizeTokenExService.ProcessPriorCaptureRequest(transData.transId, amount), false);

        private TransactionResponse ProcessCaptureOnlyRequest(TransactionRequest transactionRequest, TransactionData transData, ITokenizedPayment payment, decimal amount)
        {
            if (IsTransactionCapturedOrSettled(transData, transactionRequest))
            {
                return new TransactionResponse(null, true);
            }

            // clone the capture method in order to set InvoiceNumber
            var request = new AuthorizeNet.APICore.createCustomerProfileTransactionRequest
            {
                transaction = new AuthorizeNet.APICore.profileTransactionType
                {
                    Item = new AuthorizeNet.APICore.profileTransCaptureOnlyType
                    {
                        approvalCode = transData.AuthorizationCode,
                        customerProfileId = payment.Properties[AuthorizeTokenExGateway.ProviderProfileIdPropertyName] as string,
                        amount = amount,
                        order = new AuthorizeNet.APICore.orderExType
                        {
                            purchaseOrderNumber = transData.purchaseOrderNum,
                            invoiceNumber = transData.invoiceNum
                        }
                    }
                }
            };

            return new TransactionResponse(_authorizeTokenExService.SendRequest(request), false);
        }

        private TransactionResponse ProcessCreditRequest(TransactionRequest transactionRequest, TransactionData transData, ITokenizedPayment payment, decimal amount)
        {
            var transactionDetail = GetTransactionDetails(transData, transactionRequest);

            if (transactionDetail.transactionStatus != AuthorizeTokenExGateway.SettledStatus)
            {
                throw new PaymentException(PaymentException.ErrorType.StatusError, "", "Refund payment requires transaction status to be settled.");
            }

            // clone the Refund method in order to set InvoiceNumber
            var request = new AuthorizeNet.APICore.createCustomerProfileTransactionRequest
            {
                transaction = new AuthorizeNet.APICore.profileTransactionType
                {
                    Item = new AuthorizeNet.APICore.profileTransRefundType
                    {
                        amount = amount,
                        customerProfileId = payment.Properties[AuthorizeTokenExGateway.ProviderProfileIdPropertyName] as string,
                        transId = transData.transId,
                        order = new AuthorizeNet.APICore.orderExType
                        {
                            purchaseOrderNumber = transData.purchaseOrderNum,
                            invoiceNumber = transData.invoiceNum
                        }
                    }
                }
            };

            return new TransactionResponse(_authorizeTokenExService.SendRequest(request), false);
        }

        private AuthorizeNet.Order CreatePaymentOrder(IOrderGroup orderGroup, TransactionData transactionData, ITokenizedPayment payment, decimal orderAmount)
        {
            var customer = _authorizeTokenExService.CreateCustomer(transactionData.invoiceNum, payment.BillingAddress);

            var address = Utilities.ToAuthorizeNetAddress(payment.BillingAddress);
            customer.BillingAddress = address;
            _authorizeTokenExService.UpdateCustomer(customer);

            var shippingAddress = orderGroup.Forms.First().Shipments.First().ShippingAddress;
            if (shippingAddress == null)
            {
                throw new PaymentException(PaymentException.ErrorType.ConfigurationError, "", "Shipping address was not specified.");
            }

            var paymentProfileId = AddCreditCard(orderGroup, customer.ProfileID, payment.Token, payment.ExpirationMonth, payment.ExpirationYear);
            var shippingAddressId = AddShippingAddress(customer.ProfileID, Utilities.ToAuthorizeNetAddress(shippingAddress));

            var currency = orderGroup.Currency;
            var market = _marketService.GetMarket(orderGroup.MarketId);
            var merchantCurrency = _authorizeTokenExService.GetMerchantCurrency();
            var shippingTotal = Utilities.ConvertMoney(orderGroup.GetShippingTotal(_orderGroupCalculator), merchantCurrency);
            var salesTaxTotal = Utilities.ConvertMoney(
                new Money(orderGroup.Forms.Sum(form => form.Shipments.Sum(shipment =>
                    _shippingCalculator.GetSalesTax(shipment, market, currency).Amount)), currency).Round(),
                merchantCurrency);
            var taxTotal = Utilities.ConvertMoney(orderGroup.GetTaxTotal(_orderGroupCalculator), merchantCurrency);

            var order = new AuthorizeNet.Order(customer.ProfileID, paymentProfileId, shippingAddressId)
            {
                Amount = orderAmount - taxTotal,
                SalesTaxAmount = salesTaxTotal,
                PONumber = transactionData.purchaseOrderNum,
                InvoiceNumber = transactionData.invoiceNum,
                ShippingAmount = shippingTotal + (orderGroup.PricesIncludeTax ? 0 : taxTotal - salesTaxTotal)
            };

            var orderedLineItems = orderGroup.GetAllLineItems().OrderBy(x => x.Quantity).ToList();
            var largestQuantityItem = orderedLineItems.LastOrDefault();

            var lineItemsAmount = 0m;
            var amountWithoutLargestQtyItem = 0m;
            var settleSubTotalExclTax = orderAmount - order.ShippingAmount - order.SalesTaxAmount;
            var isFirstLineItem = true;
            var factor = (decimal)Math.Pow(10, DecimalDigit);
            var roundingDelta = 1 / factor;
            var itemPriceExcludingTaxMapping = GetPriceExcludingTax(orderGroup);

            foreach (var lineItem in orderedLineItems)
            {
                var itemCode = lineItem.Code;
                var itemQuantity = Convert.ToInt32(lineItem.Quantity);
                var displayName = string.IsNullOrEmpty(lineItem.DisplayName) ? itemCode : lineItem.DisplayName;
                var description = (lineItem as Mediachase.Commerce.Orders.LineItem)?.Description ?? string.Empty;

                if (lineItem.IsGift)
                {
                    itemCode = $"Gift: {itemCode}";
                    displayName = $"Gift: {displayName}";
                    description = $"Gift: {description}";
                }

                itemCode = Utilities.StripPreviewText(itemCode, 31);
                displayName = Utilities.StripPreviewText(displayName, 31);
                description = Utilities.StripPreviewText(description, 255);

                // Calculate unit price.
                var unitPrice = Utilities.ConvertMoney(
                        new Money(currency.Round(itemPriceExcludingTaxMapping[lineItem.LineItemId] / lineItem.Quantity, DecimalDigit), currency),
                        merchantCurrency);

                lineItemsAmount += unitPrice * lineItem.Quantity;

                if (lineItem.LineItemId != largestQuantityItem.LineItemId)
                {
                    amountWithoutLargestQtyItem = lineItemsAmount;
                }

                // In case we have Rounding Differences between rounding Total Amount (orderAmount - amount requested for settlement)
                // and rounding each lineItem unit price (amount authorized), need to recalculate unit price of item has largest quantity.
                if (lineItem.LineItemId == largestQuantityItem.LineItemId && lineItemsAmount < settleSubTotalExclTax)
                {
                    // Choose largestQuantityItem to make the unitPrice difference before and after recalculate smallest.
                    unitPrice = (settleSubTotalExclTax - amountWithoutLargestQtyItem) / largestQuantityItem.Quantity;

                    // Round up to make sure amount authorized >= requested for settlement amount.
                    unitPrice = merchantCurrency.Round(unitPrice + roundingDelta, DecimalDigit);
                }

                if (!isFirstLineItem)
                {
                    // If the lineitem price to add to the total exceed the Amount chosen
                    // for the payment. The line item price is adjusted, since it will be covered by other payment method.
                    if (unitPrice * lineItem.Quantity + order.Total > order.Amount)
                    {
                        var lineItemPrice = orderAmount - order.Total;

                        // If this is the last item, then we need to round up to make sure amount authorized >= requested for settlement amount.
                        if (lineItem.LineItemId == largestQuantityItem.LineItemId)
                        {
                            unitPrice = merchantCurrency.Round(lineItemPrice / lineItem.Quantity + roundingDelta, DecimalDigit);
                        }
                        else
                        {
                            unitPrice = merchantCurrency.Round(lineItemPrice / lineItem.Quantity, DecimalDigit);
                        }
                    }
                }

                // Note that order total calculated from authorized.net order constructor is recalculated by adding line items.
                order.AddLineItem(itemCode, displayName, description, itemQuantity, unitPrice, null);

                // We need to handle the case of first line item after being added to the order
                // since order total is recalculated when is added to the order.
                if (isFirstLineItem)
                {
                    // If with the first line item the amount chosen for the payment is exceeding
                    // the price added to the line item then should be the amount chosen.
                    if (order.Total > orderAmount)
                    {
                        // In case the order has only one line item, then we need to round up to make sure amount authorized >= requested for settlement amount.
                        if (lineItem.LineItemId == largestQuantityItem.LineItemId)
                        {
                            unitPrice = merchantCurrency.Round(settleSubTotalExclTax / lineItem.Quantity + roundingDelta, DecimalDigit);
                        }
                        else
                        {
                            unitPrice = merchantCurrency.Round(settleSubTotalExclTax / lineItem.Quantity, DecimalDigit);
                        }

                        order.RemoveLineItem(itemCode);
                        order.AddLineItem(itemCode, displayName, description, itemQuantity, unitPrice, null);
                    }
                }
                isFirstLineItem = false;
            }
            payment.Properties[AuthorizeTokenExGateway.ProviderProfileIdPropertyName] = customer.ProfileID;

            return order;
        }

        private IDictionary<int, decimal> GetPriceExcludingTax(IOrderGroup orderGroup)
        {
            var currency = orderGroup.Currency;
            var market = _marketService.GetMarket(orderGroup.MarketId);
            var itemPriceExcludingTaxMapping = new Dictionary<int, decimal>();

            foreach (var form in orderGroup.Forms)
            {
                foreach (var shipment in form.Shipments)
                {
                    var shippingAddress = shipment.ShippingAddress;
                    foreach (var lineItem in shipment.LineItems)
                    {
                        var priceExcludingTax = lineItem.GetExtendedPrice(currency).Amount;
                        if (orderGroup.PricesIncludeTax)
                        {
                            priceExcludingTax -= _lineItemCalculator.GetSalesTax(lineItem, market, currency, shippingAddress).Round().Amount;
                        }

                        itemPriceExcludingTaxMapping.Add(lineItem.LineItemId, priceExcludingTax);
                    }
                }
            }

            return itemPriceExcludingTaxMapping;
        }
    }
}
