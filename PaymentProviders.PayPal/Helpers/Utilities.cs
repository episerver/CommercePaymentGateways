using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Mediachase.Commerce.Catalog;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;

namespace PaymentProviders.PayPal
{
    internal static class Utilities
    {
        private static string _hashKey;

        private static Injected<UrlResolver> _urlResolver = default(Injected<UrlResolver>);
        private static Injected<LocalizationService> _localizationService = default(Injected<LocalizationService>);
        private static Injected<IContentRepository> _contentRepository = default(Injected<IContentRepository>);
        private static Injected<ReferenceConverter> _referenceConverter = default(Injected<ReferenceConverter>);

        /// <summary>
        /// Gets the PayPalHashKey from AppSettings. 
        /// If property is set, hashkey will be use to hash the share token (between our site and PayPal.com, use when call API to PayPal.com)
        /// </summary>
        /// <value>The hash key.</value>
        private static string HashKey
        {
            get
            {
                if (string.IsNullOrEmpty(_hashKey))
                {
                    if (!string.IsNullOrEmpty(WebConfigurationManager.AppSettings["PayPalHashKey"]))
                    {
                        _hashKey = WebConfigurationManager.AppSettings["PayPalHashKey"];
                    }
                    else
                    {
                        _hashKey = "@&*SamplePrivateHashKey!%<>?";
                    }
                }

                return _hashKey;
            }
        }

        /// <summary>
        /// Updates display name with current language.
        /// </summary>
        /// <param name="purchaseOrder">The purchase order.</param>
        public static void UpdateDisplayNameWithCurrentLanguage(IPurchaseOrder purchaseOrder)
        {
            if (purchaseOrder != null)
            {
                foreach (var lineItem in purchaseOrder.GetAllLineItems())
                {
                    lineItem.DisplayName = GetDisplayNameInCurrentLanguage(lineItem, 100);
                }
            }
        }

        /// <summary>
        /// Gets url from start page's page reference property.
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

            var result = string.Empty;
            var property = startPageData.Property[propertyName];
            if (property != null && !property.IsNull && property.Value is ContentReference)
            {
                return _urlResolver.Service.GetUrl((ContentReference)property.Value);
            }
            return string.IsNullOrEmpty(result) ? _urlResolver.Service.GetUrl(ContentReference.StartPage) : result;
        }

        /// <summary>
        /// Translates with languageKey under /Commerce/Checkout/PayPal/ in lang.xml
        /// </summary>
        /// <param name="languageKey">The language key.</param>
        public static string Translate(string languageKey)
        {
            return GetLocalizationMessage("/Commerce/Checkout/PayPal/" + languageKey);
        }

        /// <summary>
        /// Gets localized message.
        /// </summary>
        /// <param name="path">The path of the message in lang.xml file.</param>
        /// <returns></returns>
        public static string GetLocalizationMessage(string path)
        {
            return _localizationService.Service.GetString(path);
        }

        /// <summary>
        /// Strips a text to a given length without splitting the last word.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="maxLength">Max length of the text.</param>
        /// <returns>A shortened version of the given string.</returns>
        public static string StripPreviewText(string source, int maxLength)
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

        /// <summary>
        /// Gets the hash value of the accept URL by the order number.
        /// It's a MD5 key, incombination with HashKey.
        /// </summary>
        /// <param name="orderNumber">The order number.</param>
        /// <returns>The hash value.</returns>
        public static string GetAcceptUrlHashValue(string orderNumber)
        {
            return GetMD5Key(orderNumber + "accepted");
        }

        /// <summary>
        /// Gets the hash value of the cancel URL by the order number.
        /// It's a MD5 key, incombination with HashKey.
        /// </summary>
        /// <param name="orderNumber">The order number.</param>
        /// <returns>The hash value.</returns>
        public static string GetCancelUrlHashValue(string orderNumber)
        {
            return GetMD5Key(orderNumber + "canceled");
        }

        /// <summary>
        /// Gets the MD5 key, in combination with hash key string.
        /// </summary>
        /// <param name="hashString">The hash string.</param>
        /// <returns>The hash key.</returns>
        private static string GetMD5Key(string hashString)
        {
            var md5Crypto = new MD5CryptoServiceProvider();
            byte[] arrBytes = Encoding.UTF8.GetBytes(HashKey + hashString);
            arrBytes = md5Crypto.ComputeHash(arrBytes);
            var sb = new StringBuilder();
            foreach (byte b in arrBytes)
            {
                sb.Append(b.ToString("x2").ToLower());
            }
            return sb.ToString();
        }
                
        /// <summary>
        /// Gets display name of line item in current language
        /// </summary>
        /// <param name="lineItem">The line item of the order.</param>
        /// <param name="maxSize">The number of character to get display name.</param>
        /// <returns>The display name with current language.</returns>
        private static string GetDisplayNameInCurrentLanguage(ILineItem lineItem, int maxSize)
        {
            // if the entry is null (product is deleted), return item display name
            var entryContent = _contentRepository.Service.Get<EntryContentBase>(_referenceConverter.Service.GetContentLink(lineItem.Code));
            var displayName = entryContent != null ? entryContent.DisplayName : lineItem.DisplayName;
            return StripPreviewText(displayName, maxSize <= 0 ? 100 : maxSize);
        }
    }
}