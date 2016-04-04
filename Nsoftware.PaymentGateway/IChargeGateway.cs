using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Plugins.Payment;
using nsoftware.InPay;
using System;

namespace Nsoftware.PaymentGateway
{

    /// <summary>
    /// ICharge payment gateway
    /// </summary>
    public class IChargeGateway : AbstractPaymentGateway
    {
        Icharge _icharge = null;

        /// <summary>
        /// Processes the payment. Can be used for both positive and negative transactions.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public override bool ProcessPayment(Payment payment, ref string message)
        {
            var creditCardPayment = (CreditCardPayment)payment;
            _icharge = new Icharge();

            _icharge.InvoiceNumber = creditCardPayment.Parent.Parent.OrderGroupId.ToString();

            try
            {
                _icharge.MerchantLogin = Settings["MerchantLogin"];
                _icharge.MerchantPassword = Settings["MerchantPassword"];
                _icharge.Gateway = (IchargeGateways)Enum.Parse(typeof(IchargeGateways), Settings["Gateway"]);
            }
            catch
            {
                message = "ICharge gateway is not configured properly";
                return false;
            }

            if (!string.IsNullOrEmpty(Settings["GatewayURL"]))
            {
                _icharge.GatewayURL = Settings["GatewayURL"];
            }

            _icharge.Card.ExpMonth = creditCardPayment.ExpirationMonth;
            _icharge.Card.ExpYear = creditCardPayment.ExpirationYear;
            _icharge.Card.Number = creditCardPayment.CreditCardNumber;
            _icharge.Card.CVVData = creditCardPayment.CreditCardSecurityCode;

            // Find the address
            OrderAddress address = null;
            foreach (OrderAddress a in creditCardPayment.Parent.Parent.OrderAddresses)
            {
                if (string.Compare(a.Name, creditCardPayment.BillingAddressId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    address = a;
                    break;
                }
            }

            _icharge.Customer.Address = address.Line1;
            _icharge.Customer.City = address.City;
            _icharge.Customer.Country = address.CountryCode;
            _icharge.Customer.Email = address.Email;
            _icharge.Customer.FirstName = address.FirstName;
            _icharge.Customer.LastName = address.LastName;
            _icharge.Customer.Phone = address.DaytimePhoneNumber;
            _icharge.Customer.State = address.State;
            _icharge.Customer.Zip = address.PostalCode;

            double transactionAmount = (double)creditCardPayment.Amount;
            _icharge.TransactionAmount = transactionAmount.ToString();

            switch (_icharge.Gateway)
            {
                case IchargeGateways.gwAuthorizeNet:
                    AddSpecialField(creditCardPayment, "x_Trans_Key");
                    _icharge.TransactionAmount = transactionAmount.ToString("##0.00");
                    break;
                case IchargeGateways.gwPlanetPayment:
                case IchargeGateways.gwMPCS:
                case IchargeGateways.gwRTWare:
                case IchargeGateways.gwECX:
                    AddSpecialField(creditCardPayment, "x_Trans_Key");
                    AddConfigField(creditCardPayment, "AIMHashSecret");
                    break;
                case IchargeGateways.gwBankOfAmerica:
                    _icharge.AddSpecialField("ecom_payment_card_name", creditCardPayment.CustomerName);
                    AddConfigField(creditCardPayment, "referer");
                    break;
                case IchargeGateways.gwInnovative:
                    AddSpecialField(creditCardPayment, "test_override_errors");
                    break;
                case IchargeGateways.gwTrustCommerce:
                case IchargeGateways.gw3DSI:
                case IchargeGateways.gwACHPayments:
                case IchargeGateways.gwAdyen:
                case IchargeGateways.gwBarclay:
                case IchargeGateways.gwCyberbit:
                case IchargeGateways.gwFirstAtlantic:
                case IchargeGateways.gwGlobalIris:
                case IchargeGateways.gwHSBC:
                    _icharge.TransactionAmount = _icharge.TransactionAmount.Replace(".", "");
                    break;
                case IchargeGateways.gw5thDimension:
                case IchargeGateways.gwACHFederal:
                case IchargeGateways.gwAuthorizeNetXML:
                    _icharge.TransactionAmount = transactionAmount.ToString("##0.00");
                    break;
                case IchargeGateways.gwPayFuse:
                    AddConfigField(creditCardPayment, "MerchantAlias");
                    _icharge.TransactionAmount = _icharge.TransactionAmount.Replace(".", "");
                    break;
                case IchargeGateways.gwYourPay:
                case IchargeGateways.gwFirstData:
                case IchargeGateways.gwLinkPoint:
                    _icharge.SSLCert.Store = Settings["SSLCertStore"];
                    _icharge.SSLCert.Subject = Settings["SSLCertSubject"];
                    _icharge.SSLCert.Encoded = Settings["SSLCertEncoded"];
                    break;
                case IchargeGateways.gwPRIGate:
                    _icharge.MerchantPassword = Settings["MerchantPassword"];
                    break;
                case IchargeGateways.gwSagePay:
                    AddSpecialField(creditCardPayment, "RelatedSecurityKey");
                    AddSpecialField(creditCardPayment, "RelatedVendorTXCode");
                    AddSpecialField(creditCardPayment, "RelatedTXAuthNo");
                    break;
                case IchargeGateways.gwCyberCash:
                    AddSpecialField(creditCardPayment, "CustomerID");
                    AddSpecialField(creditCardPayment, "ZoneID");
                    AddSpecialField(creditCardPayment, "Username");
                    break;
                case IchargeGateways.gwPayFlowPro:
                    // for testing purpose uncomment line below   
                    //_icharge.GatewayURL = "test-payflow.verisign.com";
                    _icharge.AddSpecialField("user", Settings["MerchantLogin"]);
                    break;
                case IchargeGateways.gwMoneris:
                    // for testing purpose uncomment line below
                    //_icharge.GatewayURL = "https://esqa.moneris.com/HPPDP/index.php";
                    _icharge.TransactionAmount = transactionAmount.ToString("##0.00");
                    break;
                case IchargeGateways.gwJetPay:
                    _icharge.AddSpecialField("TerminalId", _icharge.MerchantLogin);
                    break;
                case IchargeGateways.gwNetbanx:
                    AddConfigField(creditCardPayment, "NetbanxAccountNumber");
                    break;
                case IchargeGateways.gwPayDirect:
                    AddConfigField(creditCardPayment, "PayDirectSettleMerchantCode");
                    break;
                case IchargeGateways.gwPayeezy:
                    AddConfigField(creditCardPayment, "HashSecret");
                    break;
                case IchargeGateways.gwBeanstream:
                    break;
                default:
                    break;

            }

            _icharge.TransactionDesc = String.Format("Order Number {0}", _icharge.TransactionId);
            _icharge.OnSSLServerAuthentication += new Icharge.OnSSLServerAuthenticationHandler(icharge1_SSLServerAuthentication);

            try
            {
                _icharge.Sale();

                bool approved = _icharge.Response.Approved;
                if (!approved)
                {
                    message = "Transaction Declined: " + _icharge.Response.Text;
                    return false;
                }

            }
            catch (Exception ex)
            {
                throw new GatewayNotRespondingException(ex.Message);
            }

            //info.TextResponse = _icharge.ResponseText;
            creditCardPayment.ValidationCode = _icharge.Response.ApprovalCode;
            creditCardPayment.AuthorizationCode = _icharge.Response.TransactionId;

            return true;
        }

        /// <summary>
        /// Adds the special field.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="name">The name.</param>
        private void AddSpecialField(CreditCardPayment info, string name)
        {
            string val = Settings[name];

            if (val.Length > 0)
                _icharge.AddSpecialField(name, val);
        }

        /// <summary>
        /// Adds the config field.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="name">The name.</param>
        private void AddConfigField(CreditCardPayment info, string name)
        {
            string val = Settings[name];

            if (val.Length > 0)
            {
                _icharge.Config(String.Format("{0}={1}", name, val));
            }
        }

        private void icharge1_SSLServerAuthentication(object sender, IchargeSSLServerAuthenticationEventArgs e)
        {
            //string caption = "Server Certificate" + "\r\n" + e.CertIssuer + "\r\n";
            e.Accept = true;
        }

    }
}