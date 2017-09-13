using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using System.Security.Cryptography;
using System.Text;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    public class Utilities
    {
        private static Injected<UrlResolver> _urlResolver = default(Injected<UrlResolver>);
        private static Injected<LocalizationService> _localizationService = default(Injected<LocalizationService>);
        private static Injected<IContentRepository> _catalogContentLoader = default(Injected<IContentRepository>);
        private static Injected<ReferenceConverter> _referenceConverter = default(Injected<ReferenceConverter>);

        /// <summary>
        /// Gets display name with current language.
        /// </summary>
        /// <param name="lineItem">The line item of order.</param>
        /// <param name="maxSize">The number of character to get display name.</param>
        /// <returns>Display name with current language.</returns>
        public static string GetDisplayNameOfCurrentLanguage(ILineItem lineItem, int maxSize)
        {
            // if the entry is null (product is deleted), return item display name
            var entryContent = _catalogContentLoader.Service.Get<EntryContentBase>(_referenceConverter.Service.GetContentLink(lineItem.Code));
            var displayName = entryContent != null ? entryContent.DisplayName : lineItem.DisplayName;
            return StripPreviewText(displayName, maxSize <= 0 ? 100 : maxSize);
        }

        /// <summary>
        /// Updates display name with current language.
        /// </summary>
        /// <param name="purchaseOrder">The purchase order.</param>
        public static void UpdateDisplayNameWithCurrentLanguage(IPurchaseOrder purchaseOrder)
        {
            if (purchaseOrder != null)
            {
                foreach (ILineItem lineItem in purchaseOrder.GetAllLineItems())
                {
                    lineItem.DisplayName = GetDisplayNameOfCurrentLanguage(lineItem, 100);
                }
            }
        }

        /// <summary>
        /// Gets url from start page's reference property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The friendly url.</returns>
        public static string GetUrlFromStartPageReferenceProperty(string propertyName)
        {
            var startPageData = DataFactory.Instance.GetPage(ContentReference.StartPage);
            if (startPageData == null)
            {
                return _urlResolver.Service.GetUrl(ContentReference.StartPage);
            }

            var property = startPageData.Property[propertyName];
            if (property != null && !property.IsNull && property.Value is ContentReference)
            {
                return _urlResolver.Service.GetUrl((ContentReference)property.Value);
            }
            return _urlResolver.Service.GetUrl(ContentReference.StartPage);
        }

        /// <summary>
        /// Calculates the amount, to return the smallest unit of an amount in the selected currency.
        /// </summary>
        /// <remarks>http://tech.dibspayment.com/capturecgi</remarks>
        /// <param name="currency">Selected currency</param>
        /// <param name="amount">Amount in the selected currency</param>
        /// <returns>The string represents the smallest unit of an amount in the selected currency.</returns>
        public static string GetAmount(Currency currency, decimal amount)
        {
            var delta = currency.Equals(Currency.JPY) ? 1 : 100;
            return (amount * delta).ToString("#");
        }

        /// <summary>
        /// Gets the Md5 key refund.
        /// </summary>
        /// <param name="paymentConfiguration">The DIBS payment configuration.</param>
        /// <param name="merchant">The merchant.</param>
        /// <param name="orderId">The order id.</param>
        /// <param name="transact">The transact.</param>
        /// <param name="amount">The amount.</param>
        public static string GetMD5RefundKey(DIBSConfiguration paymentConfiguration, string merchant, string orderId, string transact, string amount)
        {
            var hashString = $"merchant={merchant}&orderid={orderId}&transact={transact}&amount={amount}";
            return GetMD5Key(paymentConfiguration, hashString);
        }

        /// <summary>
        /// Gets the MD5 key used to send to DIBS in authorization step.
        /// </summary>
        /// <param name="paymentConfiguration">The DIBS payment configuration.</param>
        /// <param name="merchant">The merchant.</param>
        /// <param name="orderId">The order id.</param>
        /// <param name="currency">The currency.</param>
        /// <param name="amount">The amount.</param>
        public static string GetMD5RequestKey(DIBSConfiguration paymentConfiguration, string merchant, string orderId, Currency currency, string amount)
        {
            var hashString = $"merchant={merchant}&orderid={orderId}&currency={currency.CurrencyCode}&amount={amount}";
            return GetMD5Key(paymentConfiguration, hashString);
        }

        /// <summary>
        /// Gets the key used to verify response from DIBS when payment is approved.
        /// </summary>
        /// <param name="paymentConfiguration">The DIBS payment configuration.</param>
        /// <param name="transact">The transact.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="currency">The currency.</param>
        public static string GetMD5ResponseKey(DIBSConfiguration paymentConfiguration, string transact, string amount, Currency currency)
        {
            var hashString = $"transact={transact}&amount={amount}&currency={DIBSCurrencies.GetCurrencyCode(currency)}";
            return GetMD5Key(paymentConfiguration, hashString);
        }
        
        /// <summary>
        /// Translate with languageKey under /Commerce/Checkout/DIBS/ in lang.xml
        /// </summary>
        /// <param name="languageKey">The language key.</param>
        public static string Translate(string languageKey)
        {
            return _localizationService.Service.GetString("/Commerce/Checkout/DIBS/" + languageKey);
        }

        private static string GetMD5Key(DIBSConfiguration paymentConfiguration, string hashString)
        {
            var x = new MD5CryptoServiceProvider();
            var bs = Encoding.UTF8.GetBytes(paymentConfiguration.MD5Key1 + hashString);
            bs = x.ComputeHash(bs);
            var stringBuilder = new StringBuilder();
            foreach (byte b in bs)
            {
                stringBuilder.Append(b.ToString("x2").ToLower());
            }
            var firstHash = stringBuilder.ToString();

            var secondHash = paymentConfiguration.MD5Key2 + firstHash;
            var bs2 = Encoding.UTF8.GetBytes(secondHash);
            bs2 = x.ComputeHash(bs2);

            stringBuilder = new StringBuilder();
            foreach (byte b in bs2)
            {
                stringBuilder.Append(b.ToString("x2").ToLower());
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Strips a text to a given length without splitting the last word.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="maxLength">Max length of the text</param>
        /// <returns>A shortened version of the given string</returns>
        /// <remarks>Will return empty string if input is null or empty</remarks>
        private static string StripPreviewText(string source, int maxLength)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            if (source.Length <= maxLength)
            {
                return source;
            }

            source = source.Substring(0, maxLength);
            // The maximum number of characters to cut from the end of the string.
            var maxCharCut = (source.Length > 15 ? 15 : source.Length - 1);
            var previousWord = source.LastIndexOfAny(new char[] { ' ', '.', ',', '!', '?' }, source.Length - 1, maxCharCut);
            if (previousWord >= 0)
            {
                source = source.Substring(0, previousWord);
            }

            return source + " ...";
        }
    }
}