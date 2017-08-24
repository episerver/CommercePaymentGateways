using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    public static class CountryCodes
    {
        private static IEnumerable<string> _countriesCodes;

        static CountryCodes()
        {
            _countriesCodes = GetAllLines();
        }

        /// <summary>
        /// Gets the numeric country code.
        /// <see cref="http://unstats.un.org/unsd/methods/m49/m49alpha.htm"/>
        /// </summary>
        /// <param name="countryCode">The country code as ISO ALPHA-3 code.</param>
        public static string GetNumericCountryCode(string countryCode)
        {
            var countryCodeLine = _countriesCodes.FirstOrDefault(i => countryCode.Equals(i.Split(';')[1], StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrEmpty(countryCodeLine) ? countryCodeLine.Split(';')[2] : string.Empty;
        }

        private static string[] GetAllLines()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var result = string.Empty;

            using (var stream = assembly.GetManifestResourceStream("EPiServer.Business.Commerce.Payment.DataCash.CountriesCodes.txt"))
            {
                if (stream == null)
                {
                    return new string[] { };
                }

                using (var reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }

            return result.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToArray();
        }
    }
}