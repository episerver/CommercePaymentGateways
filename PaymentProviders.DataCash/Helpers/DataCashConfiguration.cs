using DataCash;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using System;

namespace PaymentProviders.DataCash
{
    public class DataCashConfiguration
    {
        public const string DataCashSystemName = "DataCash";

        public const string UserIdParameter = "UserIdParameter";
        public const string PasswordParameter = "PasswordParameter";
        public const string PaymentPageIdParameter = "PaymentPageId";
        public const string HostAddressParameter = "HostAddressParameter";
        public const string PathToLogFileParameter = "PathToLogFileParameter";
        public const string LoggingLevelParameter = "LogginLevelParameter";
        public const string ProxyParameter = "ProxyParameter";
        public const string TimeOutParameter = "TimeOutParameter";

        public Config Config { get; protected set; }

        public string UserId { get; protected set; }

        public string Password { get; protected set; }

        public string PaymentPageId { get; protected set; }

        public DataCashConfiguration()
        {
            Inittialize();
        }

        protected virtual void Inittialize()
        {
            var dataCashPaymentMethodDto = GetDataCashPaymentMethod();

            UserId = GetParameterValueByName(dataCashPaymentMethodDto, UserIdParameter);
            Password = GetParameterValueByName(dataCashPaymentMethodDto, PasswordParameter);
            PaymentPageId = GetParameterValueByName(dataCashPaymentMethodDto, PaymentPageIdParameter);
            Config = GetConfig(dataCashPaymentMethodDto);
        }

        public static PaymentMethodDto GetDataCashPaymentMethod()
        {
            return PaymentManager.GetPaymentMethodBySystemName(DataCashSystemName, SiteContext.Current.LanguageName);
        }

        public static PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(PaymentMethodDto paymentMethodDto, string parameterName)
        {
            var rowArray = (PaymentMethodDto.PaymentMethodParameterRow[])paymentMethodDto.PaymentMethodParameter.Select($"Parameter = '{parameterName}'");
            return rowArray.Length > 0 ? rowArray[0] : null;
        }

        public static string GetParameterValueByName(PaymentMethodDto paymentMethodDto, string name)
        {
            var param = GetParameterByName(paymentMethodDto, name);
            return param != null ? param.Value : String.Empty;
        }

        /// <summary>
        /// Gets the configured object from payment configuration.
        /// </summary>
        private Config GetConfig(PaymentMethodDto paymentMethodDto)
        {
            var cfg = new Config();
            cfg.setHost(GetParameterValueByName(paymentMethodDto, HostAddressParameter));

            int intResult;
            var value = GetParameterValueByName(paymentMethodDto, PathToLogFileParameter);
            if (!string.IsNullOrEmpty(value))
            {
                cfg.setLogfile(value);

                // log level can be set only if log file path was set
                int.TryParse(GetParameterValueByName(paymentMethodDto, LoggingLevelParameter), out intResult);
                cfg.setLogLevel(intResult);
            }

            value = GetParameterValueByName(paymentMethodDto, ProxyParameter);
            if (!string.IsNullOrEmpty(value))
            {
                cfg.setProxy(value);
            }

            int.TryParse(GetParameterValueByName(paymentMethodDto, TimeOutParameter), out intResult);
            if (intResult <= 0)
            {
                intResult = 60;
            }
            cfg.setTimeout(intResult);

            return cfg;
        }
    }
}