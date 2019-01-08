using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    public class Utilities
    {
        private static Injected<UrlResolver> _urlResolver = default(Injected<UrlResolver>);
        private static Injected<LocalizationService> _localizationService = default(Injected<LocalizationService>);
        private static Injected<ReferenceConverter> _referenceConverter = default(Injected<ReferenceConverter>);
        private static Injected<IContentLoader> _contentLoader = default(Injected<IContentLoader>);

        /// <summary>
        /// Gets display name with current language.
        /// </summary>
        /// <param name="lineItem">The line item of order.</param>
        /// <param name="maxSize">The number of character to get display name.</param>
        /// <returns>Display name with current language.</returns>
        public static string GetDisplayNameOfCurrentLanguage(ILineItem lineItem, int maxSize)
        {
            // if the entry is null (product is deleted), return item display name
            var entryContent = _contentLoader.Service.Get<EntryContentBase>(_referenceConverter.Service.GetContentLink(lineItem.Code));
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
            var startPageData = _contentLoader.Service.Get<PageData>(ContentReference.StartPage);
            if (startPageData == null)
            {
                return _urlResolver.Service.GetUrl(ContentReference.StartPage);
            }

            var contentLink = startPageData.Property[propertyName]?.Value as ContentReference;
            if (!ContentReference.IsNullOrEmpty(contentLink))
            {
                return _urlResolver.Service.GetUrl(contentLink);
            }
            return _urlResolver.Service.GetUrl(ContentReference.StartPage);
        }

        /// <summary>
        /// Calculates the amount, to return the smallest unit of an amount in the selected currency.
        /// </summary>
        /// <param name="currency">Selected currency</param>
        /// <param name="amount">Amount in the selected currency</param>
        /// <returns>The smallest unit of an amount in the selected currency.</returns>
        public static decimal GetAmount(Currency currency, decimal amount)
        {
            var delta = currency.Equals(Currency.JPY) ? 1 : 100;
            return Math.Round(amount * delta, 0);
        }

        /// <summary>
        /// Gets the MD5 key used to send to DIBS in authorization step.
        /// </summary>
        /// <param name="paymentConfiguration">The DIBS payment configuration.</param>
        /// <param name="keyPairs">The key pairs.</param>
        public static string GetMACRequest(DIBSConfiguration paymentConfiguration, Dictionary<string, object> keyPairs)
        {
            var messageString = string.Join("&", keyPairs.Select(kp => $"{kp.Key}={kp.Value}"));

            return GetHMACCalculation(paymentConfiguration, messageString);
        }

        /// <summary>
        /// Translate with languageKey under /Commerce/Checkout/DIBS/ in lang.xml
        /// </summary>
        /// <param name="languageKey">The language key.</param>
        public static string Translate(string languageKey) => _localizationService.Service.GetString("/Commerce/Checkout/DIBS/" + languageKey);

        private static string GetHMACCalculation(DIBSConfiguration paymentConfiguration, string messageString)
        {
            // reference: https://tech.dibspayment.com/batch/d2integratedpwapihmac

            //Decoding the secret Hex encoded key and getting the bytes for MAC calculation
            var hmacKeyBytes = new byte[paymentConfiguration.HMACKey.Length / 2];
            for (var i = 0; i < hmacKeyBytes.Length; i++)
            {
                hmacKeyBytes[i] = byte.Parse(paymentConfiguration.HMACKey.Substring(i * 2, 2), NumberStyles.HexNumber);
            }

            var msgBytes = Encoding.UTF8.GetBytes(messageString);

            //Calculate MAC key
            var hash = new HMACSHA256(hmacKeyBytes);
            var macBytes = hash.ComputeHash(msgBytes);
            return BitConverter.ToString(macBytes).Replace("-", string.Empty).ToLower();
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