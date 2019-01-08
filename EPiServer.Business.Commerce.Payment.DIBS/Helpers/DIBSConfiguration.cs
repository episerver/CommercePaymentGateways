using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    /// <summary>
    /// Represents DIBS configuration data.
    /// </summary>
    public class DIBSConfiguration
    {
        public const string UserParameter = "MerchantID";
        public const string PasswordParameter = "Password";
        public const string ProcessingUrlParamter = "ProcessingUrl";
        public const string HMACKeyParameter = "HMACKey";

        public const string DIBSSystemName = "DIBS";

        private PaymentMethodDto _paymentMethodDto;
        private IDictionary<string, string> _settings;

        /// <summary>
        /// Gets the payment method ID.
        /// </summary>
        public Guid PaymentMethodId { get; protected set; }

        /// <summary>
        /// Gets the merchant.
        /// </summary>
        public string Merchant { get; protected set; }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password { get; protected set; }

        /// <summary>
        /// Gets the progressing Url.
        /// </summary>
        public string ProcessingUrl { get; protected set; }

        /// <summary>
        /// Gets the HMAC key setting.
        /// </summary>
        public string HMACKey { get; protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="DIBSConfiguration"/>.
        /// </summary>
        public DIBSConfiguration():this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DIBSConfiguration"/> with specific settings.
        /// </summary>
        /// <param name="settings">The specific settings.</param>
        public DIBSConfiguration(IDictionary<string, string> settings)
        {
            Initialize(settings);
        }

        /// <summary>
        /// Gets the PaymentMethodDto's parameter (setting in CommerceManager of DIBS) by name.
        /// </summary>
        /// <param name="paymentMethodDto">The payment method dto.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The parameter row.</returns>
        public static PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(PaymentMethodDto paymentMethodDto, string parameterName)
        {
            var rowArray = (PaymentMethodDto.PaymentMethodParameterRow[])paymentMethodDto.PaymentMethodParameter.Select(string.Format("Parameter = '{0}'", parameterName));
            return rowArray.Length > 0 ? rowArray[0] : null;
        }

        /// <summary>
        /// Gets the PaymentMethodDto of DIBS.
        /// </summary>
        /// <returns>The DIBS payment method.</returns>
        public static PaymentMethodDto GetDIBSPaymentMethod()
        {
            return PaymentManager.GetPaymentMethodBySystemName(DIBSSystemName, SiteContext.Current.LanguageName);
        }

        protected virtual void Initialize(IDictionary<string, string> settings)
        {
            _paymentMethodDto = GetDIBSPaymentMethod();
            PaymentMethodId = GetPaymentMethodId();

            _settings = settings ?? GetSettings();
            GetParametersValues();
        }

        private IDictionary<string, string> GetSettings()
        {
            return _paymentMethodDto.PaymentMethod
                                    .FirstOrDefault()
                                   ?.GetPaymentMethodParameterRows()
                                   ?.ToDictionary(row => row.Parameter, row => row.Value);
        }

        private void GetParametersValues()
        {
            if (_settings != null)
            {
                Merchant = GetParameterValue(UserParameter);
                Password = GetParameterValue(PasswordParameter);
                ProcessingUrl = GetParameterValue(ProcessingUrlParamter);
                HMACKey = GetParameterValue(HMACKeyParameter);
            }
        }
        private string GetParameterValue(string parameterName)
        {
            return _settings.TryGetValue(parameterName, out var parameterValue) ? parameterValue : string.Empty;
        }

        private Guid GetPaymentMethodId()
        {
            var dibsPaymentMethodRow = _paymentMethodDto.PaymentMethod.Rows[0] as PaymentMethodDto.PaymentMethodRow;
            var paymentMethodId = dibsPaymentMethodRow != null ? dibsPaymentMethodRow.PaymentMethodId : Guid.Empty;
            return paymentMethodId;
        }
    }
}
