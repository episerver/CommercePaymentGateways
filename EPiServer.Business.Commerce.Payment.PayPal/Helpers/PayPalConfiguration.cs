using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPiServer.Business.Commerce.Payment.PayPal
{
    /// <summary>
    /// Represents PayPal configuration data.
    /// </summary>
    public class PayPalConfiguration
    {
        private PaymentMethodDto _paymentMethodDto;
        private IDictionary<string, string> _settings;

        public const string PayPalSystemName = "PayPal";

        public const string BusinessEmailParameter = "PayPalBusinessEmail";
        public const string AllowChangeAddressParameter = "PayPalChangeAddress";
        public const string AllowGuestParameter = "PayPalAllowGuest";
        public const string PaymentActionParameter = "PayPalPaymentAction";
        public const string UserParameter = "PayPalAPIUser";
        public const string PasswordParameter = "PayPalAPIPassword";
        public const string APISignatureParameter = "PayPalAPISignature";
        public const string PALParameter = "PayPalPAL";
        public const string SandBoxParameter = "PayPalSandBox";
        public const string ExpChkoutURLParameter = "PayPalExpChkoutURL";
        public const string SkipConfirmPageParameter = "SkipConfirmPage";

        public Guid PaymentMethodId { get; protected set; }

        public string BusinessEmail { get; protected set; }

        public string AllowChangeAddress { get; protected set; }

        public string AllowGuest { get; protected set; }

        public string PaymentAction { get; protected set; }

        public string User { get; protected set; }

        public string Password { get; protected set; }

        public string APISignature { get; protected set; }

        public string PAL { get; protected set; }

        public string SandBox { get; protected set; }

        public string ExpChkoutURL { get; protected set; }

        public string SkipConfirmPage { get; protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="PayPalConfiguration"/>.
        /// </summary>
        public PayPalConfiguration() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PayPalConfiguration"/> with specific settings.
        /// </summary>
        /// <param name="settings">The specific settings.</param>
        public PayPalConfiguration(IDictionary<string, string> settings)
        {
            Initialize(settings);
        }

        /// <summary>
        /// Gets the PaymentMethodDto's parameter (setting in CommerceManager of PayPal) by name.
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
        /// Returns the PaymentMethodDto of PayPal.
        /// </summary>
        /// <returns>The PayPal payment method.</returns>
        public static PaymentMethodDto GetPayPalPaymentMethod()
        {
            return PaymentManager.GetPaymentMethodBySystemName(PayPalSystemName, SiteContext.Current.LanguageName);
        }

        protected virtual void Initialize(IDictionary<string, string> settings)
        {
            _paymentMethodDto = GetPayPalPaymentMethod();
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
                BusinessEmail = GetParameterValue(BusinessEmailParameter);
                AllowChangeAddress = GetParameterValue(AllowChangeAddressParameter);
                AllowGuest = GetParameterValue(AllowGuestParameter);
                PaymentAction = GetParameterValue(PaymentActionParameter);
                User = GetParameterValue(UserParameter);
                Password = GetParameterValue(PasswordParameter);
                APISignature = GetParameterValue(APISignatureParameter);
                PAL = GetParameterValue(PALParameter);
                SandBox = GetParameterValue(SandBoxParameter);
                ExpChkoutURL = GetParameterValue(ExpChkoutURLParameter);
                SkipConfirmPage = GetParameterValue(SkipConfirmPageParameter);
            }
        }

        private string GetParameterValue(string parameterName)
        {
            string parameterValue;
            return _settings.TryGetValue(parameterName, out parameterValue) ? parameterValue : string.Empty;
        }

        private Guid GetPaymentMethodId()
        {
            var paymentMethodRow = _paymentMethodDto.PaymentMethod.Rows[0] as PaymentMethodDto.PaymentMethodRow;
            return paymentMethodRow != null ? paymentMethodRow.PaymentMethodId : Guid.Empty;
        }
    }
}
