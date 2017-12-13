using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    public static class CountryCodes
    {
        private static readonly Lazy<string[]> _countriesCodes = new Lazy<string[]>(GetAllLines);

        /// <summary>
        /// Gets the numeric country code.
        /// <see cref="http://unstats.un.org/unsd/methods/m49/m49alpha.htm"/>
        /// </summary>
        /// <param name="countryCode">The country code as ISO ALPHA-3 code.</param>
        public static string GetNumericCountryCode(string countryCode)
        {
            var countryCodeLine = _countriesCodes.Value.FirstOrDefault(i => countryCode.Equals(i.Split(';')[1], StringComparison.OrdinalIgnoreCase));
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

            // The result returned by reading stream above uses different new line character in some cases, it could be "\r\n" or "\n"
            // and the Environment.NewLine is also different in different platform ("\r\n" in Windows platform and "\n" in Unix platform).
            return result.Split(new[] { Environment.NewLine, "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToArray();
        }
    }
}