using System;
using System.Linq;
using System.Web;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using System.Globalization;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders;
using System.Collections.Generic;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Markets;

namespace Nsoftware.PaymentGateway
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class IChargeInitialization : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var paymentMethodDto = PaymentManager.GetPaymentMethods("en");
            var paymentMethod = paymentMethodDto.PaymentMethod
                .FirstOrDefault(method =>method.SystemKeyword.Equals("iCharge", StringComparison.InvariantCultureIgnoreCase));

            if (paymentMethod == null)
            {
                ConfigurePaymentMethod(paymentMethodDto);
            }
            else
            {
                if (!paymentMethod.ClassName.Equals("Nsoftware.PaymentGateway.IChargeGateway, Nsoftware.PaymentGateway"))
                {
                    paymentMethod.ClassName = "Nsoftware.PaymentGateway.IChargeGateway, Nsoftware.PaymentGateway";
                }
            }

            if (paymentMethodDto.HasChanges())
            {
                PaymentManager.SavePayment(paymentMethodDto);
            }
        }

        public void Uninitialize(InitializationEngine context)
        {

        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            
        }

        private void ConfigurePaymentMethod(PaymentMethodDto paymentMethodDto)
        {
            var marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            var allMarkets = marketService.GetAllMarkets().Where(x => x.IsEnabled).ToList();
            foreach (var language in allMarkets.SelectMany(x => x.Languages).Distinct())
            {
                AddPaymentMethod(Guid.NewGuid(),
                    "Credit card",
                    "ICharge",
                    "Credit card payment",
                    "Mediachase.Commerce.Orders.CreditCardPayment, Mediachase.Commerce",
                    "Nsoftware.PaymentGateway.IChargeGateway, Nsoftware.PaymentGateway",
                    true, 1, allMarkets, language, paymentMethodDto);

            }
        }

        private static void AddPaymentMethod(Guid id, string name, string systemKeyword, string description, string implementationClass, string gatewayClass,
            bool isDefault, int orderIndex, IEnumerable<IMarket> markets, CultureInfo language, PaymentMethodDto paymentMethodDto)
        {
            var row = paymentMethodDto.PaymentMethod.AddPaymentMethodRow(id, name, description, language.TwoLetterISOLanguageName,
                            systemKeyword, true, isDefault, gatewayClass,
                            implementationClass, false, orderIndex, DateTime.Now, DateTime.Now, AppContext.Current.ApplicationId);

            var paymentMethod = new PaymentMethod(row);
            paymentMethod.MarketId.AddRange(markets.Where(x => x.IsEnabled && x.Languages.Contains(language)).Select(x => x.MarketId));
            paymentMethod.SaveChanges();
        }
    }
}