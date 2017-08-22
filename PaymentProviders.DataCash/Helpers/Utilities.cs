﻿using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Configuration;

namespace PaymentProviders.DataCash
{
    internal static class Utilities
    {
        // The unique system name of DataCash payment module, which has been configured Payment section in Commerce Manager
        private static string _hashKey;
        private static Injected<UrlResolver> _urlResolver =  default(Injected<UrlResolver>);
        private static Injected<LocalizationService> _localizationService = default(Injected<LocalizationService>);

        /// <summary>
        /// Translate with languageKey under /Commerce/Checkout/DataCash/ in lang.xml
        /// </summary>
        public static string Translate(string languageKey)
        {
            return _localizationService.Service.GetString("/Commerce/Checkout/DataCash/" + languageKey);
        }

        /// <summary>
        /// Gets client IP v4 address.
        /// </summary>
        public static string GetIPAddress(HttpRequest request)
        {
            var ipString = !string.IsNullOrEmpty(request.ServerVariables["HTTP_VIA"]) 
                ? request.ServerVariables["HTTP_X_FORWARDED_FOR"] // Web user - if using proxy
                : request.ServerVariables["REMOTE_ADDR"]; // Web user - not using proxy or can't get the Client IP

            // If we can't get a V4 IP from the above, try host address list for internal users.
            if (!IsIPV4(ipString))
            {
                var ipAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => IsIPV4(ip))?.ToString();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    ipString = ipAddress;
                }
            }

            return ipString;
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

            var property = startPageData.Property[propertyName];
            if (property != null && !property.IsNull && property.Value is ContentReference)
            {
                return _urlResolver.Service.GetUrl((ContentReference)property.Value);
            }
            return _urlResolver.Service.GetUrl(ContentReference.StartPage);
        }

        /// <summary>
        /// Gets the MD5 key, in combination with HashKey.
        /// </summary>
        /// <param name="hashString">The hash string.</param>
        /// <returns>The hash key.</returns>
        public static string GetMD5Key(string hashString)
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
        /// Gets the DataCashHashKey from AppSettings. 
        /// If property is set, hashkey will be use to hash the share token (between our site and DataCash.com, use when call API to DataCash.com)
        /// </summary>
        private static string HashKey
        {
            get
            {
                if (string.IsNullOrEmpty(_hashKey))
                {
                    _hashKey = !string.IsNullOrEmpty(WebConfigurationManager.AppSettings["PayPalHashKey"])
                        ? WebConfigurationManager.AppSettings["PayPalHashKey"]
                        : "@&*SamplePrivateHashKey!%<>?";
                }

                return _hashKey;
            }
        }

        private static bool IsIPV4(string input)
        {
            IPAddress address;
            return IPAddress.TryParse(input, out address) && IsIPV4(address);
        }

        private static bool IsIPV4(IPAddress ipAddress)
        {
            return ipAddress.AddressFamily == AddressFamily.InterNetwork;
        }
    }
}