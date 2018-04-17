using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using PayPal.PayPalAPIInterfaceService;
using PayPal.PayPalAPIInterfaceService.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EPiServer.Business.Commerce.Payment.PayPal
{
    public class PayPalAPIHelper
    {
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly LocalizationService _localizationService;
        private readonly PayPalCurrencies _paypalCurrencies;

        public PayPalAPIHelper() : this(
            ServiceLocator.Current.GetInstance<IOrderGroupCalculator>(),
            ServiceLocator.Current.GetInstance<LocalizationService>(),
            new PayPalCurrencies())
        { }

        [Obsolete("This constructor is no longer used, use constructor with IOrderGroupCalculator instead. Will remain at least until November 2018.")]
        public PayPalAPIHelper(
            IShippingCalculator shippingCalculator,
            ITaxCalculator taxCalculator,
            LocalizationService localizationService,
            PayPalCurrencies paypalCurrencies)
            : this (ServiceLocator.Current.GetInstance<IOrderGroupCalculator>(),
                  localizationService,
                  paypalCurrencies)
        {
        }

        public PayPalAPIHelper(
            IOrderGroupCalculator orderGroupCalculator,
            LocalizationService localizationService,
            PayPalCurrencies paypalCurrencies)
        {
            _orderGroupCalculator = orderGroupCalculator;
            _localizationService = localizationService;
            _paypalCurrencies = paypalCurrencies;
        }

        /// <summary>
        /// Setup the PayPal API caller service, use the profile setting with pre-configured parameters.
        /// </summary>
        /// <param name="payPalConfiguration">The PayPal payment configuration.</param>
        public static PayPalAPIInterfaceServiceService GetPayPalAPICallerServices(PayPalConfiguration payPalConfiguration)
        {
            var configMap = new Dictionary<string, string>();
            configMap.Add("mode", payPalConfiguration.SandBox == "1" ? "sandbox" : "live");

            // Signature Credential
            configMap.Add("account1.apiUsername", payPalConfiguration.User);
            configMap.Add("account1.apiPassword", payPalConfiguration.Password);
            configMap.Add("account1.apiSignature", payPalConfiguration.APISignature);

            return new PayPalAPIInterfaceServiceService(configMap);
        }

        /// <summary>
        /// Converts value to PayPal amount type.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="currency">The currency id.</param>
        /// <returns>The basic amount type of PayPal, with 2 decimal digits value.</returns>
        public BasicAmountType ToPayPalAmount(decimal amount, Currency currency)
        {
            var currencyId = _paypalCurrencies.GetCurrencyCode(currency);
            return new BasicAmountType { value = amount.ToString("0.00", CultureInfo.InvariantCulture), currencyID = currencyId };
        }

        /// <summary>
        /// Checks the PayPal API response for errors.
        /// </summary>
        /// <param name="abstractResponse">the PayPal API response.</param>
        /// <returns>The error message list(s) when abstractResponse.Ack is not Success or SuccessWithWarning. When everything OK, return string.Empty.</returns>
        public string CheckErrors(AbstractResponseType abstractResponse)
        {
            var errorList = string.Empty;

            // First, check the Obvious.  Make sure Ack is not Success
            if (abstractResponse.Ack != AckCodeType.SUCCESS && abstractResponse.Ack != AckCodeType.SUCCESSWITHWARNING)
            {
                // The value returned in CorrelationID is important for PayPal to determine the precise cause of any error you might encounter. 
                // If you have to troubleshoot a problem with your requests, capture the value of CorrelationID so you can report it to PayPal.
                errorList = $"PayPal API {abstractResponse.Version}.{abstractResponse.Build}: [{abstractResponse.Ack.ToString()}] CorrelationID={abstractResponse.CorrelationID}.\n";

                if (abstractResponse.Errors.Count > 0)
                {
                    foreach (var error in abstractResponse.Errors)
                    {
                        errorList += $"\n[{error.SeverityCode.ToString()}-{error.ErrorCode}]: {error.LongMessage}.";
                    }
                }
                else
                {
                    errorList += _localizationService.GetString("/Commerce/Checkout/PayPal/PayPalAPICallError");
                }
            }

            return errorList;
        }

        /// <summary>
        /// Construct the Gets the PayPal payment details from our payment and Cart to pass onto PayPal.
        /// </summary>
        /// <remarks>
        /// The PayPal payment detail can be a bit different from OrderForm because 
        /// sometimes cart total calculated by Commerce is different with cart total calculated by PalPal, 
        /// though this difference is very small (~0.01 currency).
        /// We adjust this into an additional item to ensure PayPal shows the same total number with Commerce.
        /// We also add the Order discount (if any) as and special item with negative price to PayPal payment detail.
        /// See detail about PayPal structure type in this link <seealso cref="https://cms.paypal.com/mx/cgi-bin/?cmd=_render-content&content_ID=developer/e_howto_api_soap_r_GetExpressCheckoutDetails#id0848IH0064Y__id099MB0BB0UX"/>
        /// </remarks>
        /// <param name="payment">The payment to take info (Total, LineItem, ...) from</param>
        /// <param name="orderGroup">The order group (to be InvoiceID to pass to PayPal)</param>
        /// <param name="orderNumber">The order number.</param>
        /// <param name="notifyUrl">The notify Url.</param>
        /// <returns>The PayPal payment detail to pass to API request</returns>
        public PaymentDetailsType GetPaymentDetailsType(IPayment payment, IOrderGroup orderGroup, string orderNumber, string notifyUrl)
        {
            var orderForm = orderGroup.Forms.First(form => form.Payments.Contains(payment));
            var paymentDetailsType = new PaymentDetailsType();

            paymentDetailsType.ButtonSource = "Episerver_Cart_EC"; // (Optional) An identification code for use by third-party applications to identify transactions. Character length and limitations: 32 single-byte alphanumeric characters
            paymentDetailsType.InvoiceID = orderNumber;  // Character length and limitations: 127 single-byte alphanumeric characters
            paymentDetailsType.Custom = orderGroup.CustomerId + "|" + paymentDetailsType.InvoiceID; // A free-form field for your own use. Character length and limitations: 256 single-byte alphanumeric characters
                                                                                                    // NOTE: paymentDetailsType.OrderDescription = 127 single-byte alphanumeric characters string
                                                                                                    // NOTE: paymentDetailsType.TransactionId = string, provided if you have transactionId in your Commerce system // (Optional) Transaction identification number of the transaction that was created.;

            // (Optional) Your URL for receiving Instant Payment Notification (IPN) about this transaction. If you do not specify this value in the request, the notification URL from your Merchant Profile is used, if one exists. 
            // IMPORTANT:The notify URL only applies to DoExpressCheckoutPayment. This value is ignored when set in SetExpressCheckout or GetExpressCheckoutDetails.
            // Character length and limitations: 2,048 single-byte alphanumeric characters
            paymentDetailsType.NotifyURL = notifyUrl;

            var currency = orderGroup.Currency;
            var totalOrder = currency.Round(payment.Amount);
            var totalShipping = currency.Round(orderGroup.GetShippingTotal(_orderGroupCalculator).Amount);
            var totalHandling = currency.Round(orderForm.HandlingTotal);
            var totalTax = currency.Round(orderGroup.GetTaxTotal(_orderGroupCalculator).Amount);
            var lineItemTotal = 0m;

            var paymentDetailItems = new List<PaymentDetailsItemType>();
            foreach (var lineItem in orderForm.GetAllLineItems())
            {
                // recalculate final unit price after all kind of discounts are subtracted from item.ListPrice
                var finalUnitPrice = currency.Round(lineItem.GetExtendedPrice(currency).Amount / lineItem.Quantity);
                lineItemTotal += finalUnitPrice * lineItem.Quantity;

                paymentDetailItems.Add(new PaymentDetailsItemType
                {
                    Name = lineItem.DisplayName,
                    Number = lineItem.Code,
                    Quantity = Convert.ToInt32(lineItem.Quantity.ToString("0")),
                    Amount = ToPayPalAmount(finalUnitPrice, currency)
                });
            }

            // this adjustment also include the gift-card (in sample)
            var orderAdjustment = totalOrder - totalShipping - totalHandling - totalTax - lineItemTotal;
            var adjustmentForShipping = 0m;
            if (orderAdjustment != 0  // adjustment for gift card/(order level) promotion case
                || lineItemTotal == 0 // in this case, the promotion (or discount) make all lineItemTotal zero, but buyer still have to pay shipping (and/or handling, tax). 
                                      // We still need to adjust lineItemTotal for Paypal accepting (need to be greater than zero)
                )
            {
                var paymentDetailItem = new PaymentDetailsItemType
                {
                    Name = "Order adjustment",
                    Number = "ORDERADJUSTMENT",
                    Description = "GiftCard, Discount at OrderLevel and/or PayPal-Commerce-calculating difference in cart total",  // Character length and limitations: 127 single-byte characters
                    Quantity = 1
                };

                var predictLineitemTotal = lineItemTotal + orderAdjustment;
                if (predictLineitemTotal <= 0)
                {
                    // can't overpaid for item. E.g.: total item amount is 68, orderAdjustment is -70, PayPal will refuse ItemTotal = -2
                    // we need to push -2 to shippingTotal or shippingDiscount

                    // HACK: Paypal will not accept an item total of $0, even if there is a shipping fee. The Item total must be at least 1 cent/penny. 
                    // We need to take 1 cent/penny from adjustmentForLineItemTotal and push to adjustmentForShipping
                    orderAdjustment = (-lineItemTotal + 0.01m); // -68 + 0.01 = -67.99
                    adjustmentForShipping = predictLineitemTotal - 0.01m; // -2 - 0.01 = -2.01
                }
                else
                {
                    // this case means: due to PayPal calculation, buyer need to pay more that what Commerce calculate. Because:
                    // sometimes cart total calculated by Commerce is different with
                    // cart total calculated by PalPal, though this difference is very small (~0.01 currency)
                    // We adjust the items total to make up for that, to ensure PayPal shows the same total number with Commerce
                }

                lineItemTotal += orderAdjustment; // re-adjust the lineItemTotal

                paymentDetailItem.Amount = ToPayPalAmount(orderAdjustment, currency);
                paymentDetailItems.Add(paymentDetailItem);
            }

            if (adjustmentForShipping > 0)
            {
                totalShipping += adjustmentForShipping;
            }
            else
            {
                // Shipping discount for this order. You specify this value as a negative number.
                // NOTE:Character length and limitations: Must not exceed $10,000 USD in any currency. 
                // No currency symbol. Regardless of currency, decimal separator must be a period (.), and the optional thousands separator must be a comma (,). 
                // Equivalent to nine characters maximum for USD. 
                // NOTE:You must set the currencyID attribute to one of the three-character currency codes for any of the supported PayPal currencies.
                paymentDetailsType.ShippingDiscount = ToPayPalAmount(adjustmentForShipping, currency);
            }

            paymentDetailsType.OrderTotal = ToPayPalAmount(totalOrder, currency);
            paymentDetailsType.ShippingTotal = ToPayPalAmount(totalShipping, currency);
            paymentDetailsType.HandlingTotal = ToPayPalAmount(totalHandling, currency);
            paymentDetailsType.TaxTotal = ToPayPalAmount(totalTax, currency);
            paymentDetailsType.ItemTotal = ToPayPalAmount(lineItemTotal, currency);

            paymentDetailsType.PaymentDetailsItem = paymentDetailItems;
            paymentDetailsType.ShipToAddress = AddressHandling.ToAddressType(orderForm.Shipments.First().ShippingAddress);

            if (orderForm.Shipments.Count() > 1)
            {
                // (Optional) The value 1 indicates that this payment is associated with multiple shipping addresses. Character length and limitations: Four single-byte numeric characters.
                paymentDetailsType.MultiShipping = "1";
            }

            return paymentDetailsType;
        }

        public SetExpressCheckoutRequestDetailsType CreateExpressCheckoutReqDetailsType(IPayment payment, PayPalConfiguration payPalConfiguration)
        {
            var setExpressChkOutReqDetails = new SetExpressCheckoutRequestDetailsType
            {
                BillingAddress = AddressHandling.ToAddressType(payment.BillingAddress),
                BuyerEmail = payment.BillingAddress.Email
            };

            TransactionType transactionType;
            if (Enum.TryParse(payment.TransactionType, out transactionType))
            {
                if (transactionType == TransactionType.Authorization)
                {
                    setExpressChkOutReqDetails.PaymentAction = PaymentActionCodeType.AUTHORIZATION;
                }
                else if (transactionType == TransactionType.Sale)
                {
                    setExpressChkOutReqDetails.PaymentAction = PaymentActionCodeType.SALE;
                }
            }

            if (payPalConfiguration.AllowChangeAddress != "1")
            {
                setExpressChkOutReqDetails.AddressOverride = "1";
            }

            if (payPalConfiguration.AllowGuest == "1")
            {
                setExpressChkOutReqDetails.SolutionType = SolutionTypeType.SOLE;
                setExpressChkOutReqDetails.LandingPage = LandingPageType.BILLING;
            }

            return setExpressChkOutReqDetails;
        }

        /// <summary>
        /// Processes the checkout details from PayPal, update changed addresses appropriately.
        /// </summary>
        /// <param name="payerInfo">The PayPal payer info.</param>
        /// <param name="payPalAddress">The PayPal address.</param>
        /// <param name="orderAddress">The order address to process.</param>
        /// <param name="customerAddressType">The order address to process.</param>
        /// <param name="emptyAddressMsgKey">The empty address message language key in lang.xml file.</param>
        public string ProcessOrderAddress(PayerInfoType payerInfo, AddressType payPalAddress, IOrderAddress orderAddress, CustomerAddressTypeEnum customerAddressType, string emptyAddressMsgKey)
        {
            if (payPalAddress == null)
            {
                return _localizationService.GetString(emptyAddressMsgKey);
            }

            if (orderAddress == null)
            {
                return _localizationService.GetString("/Commerce/Checkout/PayPal/CommitTranErrorCartReset");
            }

            if (string.IsNullOrEmpty(payPalAddress.Phone) && !string.IsNullOrEmpty(payerInfo?.ContactPhone))
            {
                payPalAddress.Phone = payerInfo.ContactPhone;
            }

            AddressHandling.UpdateOrderAddress(orderAddress, customerAddressType, payPalAddress, payerInfo.Payer);

            return string.Empty;
        }
    }
}
