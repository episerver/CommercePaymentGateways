using PayPal.PayPalAPIInterfaceService.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace PaymentProviders.PayPal
{
    /// <summary>
    /// Handles countries and states process.
    /// </summary>
    public static class CountriesAndStates
    {
        class Country
        {
            public string ISO2 { get; }
            public string ISO3 { get; }
            public string Name { get; }

            public Country(string iso2, string iso3, string name)
            {
                ISO2 = iso2;
                ISO3 = iso3;
                Name = name;
            }
        }

        class State
        {
            public string Code { get; }
            public string Name { get; }

            public State(string code, string name)
            {
                Code = code;
                Name = name;
            }
        }
        
        private static IEnumerable<Country> _countries;
        
        private static IEnumerable<State> _canadianUSStates;

        static CountriesAndStates()
        {
            _countries = GetCountryCodes();
            _canadianUSStates = GetStates();
        }

        /// <summary>
        /// Gets the Alpha 3 name by country alpha 2 code.
        /// </summary>
        /// <param name="countryAlpha2Code">The country alpha 2 code.</param>
        /// <returns>The Alpha 3 name.</returns>
        public static string GetAlpha3CountryCode(string countryAlpha2Code)
        {
            var country = _countries.FirstOrDefault(c => c.ISO2.Equals(countryAlpha2Code, StringComparison.OrdinalIgnoreCase));
            return country != null ? country.ISO3 : string.Empty;
        }

        /// <summary>
        /// Gets <see cref="CountryCodeType"/> by country alpha 3 code.
        /// </summary>
        /// <param name="countryAlpha3Code">The country alpha 3 code.</param>
        /// <returns>The <see cref="CountryCodeType"/>.</returns>
        public static CountryCodeType GetAlpha2CountryCode(string countryAlpha3Code)
        {
            var country = _countries.FirstOrDefault(c => c.ISO3.Equals(countryAlpha3Code, StringComparison.OrdinalIgnoreCase));
            var code = country != null ? country.ISO2 : string.Empty;

            if (string.IsNullOrEmpty(code))
            {
                return CountryCodeType.CUSTOMCODE;
            }

            CountryCodeType result;
            if (Enum.TryParse<CountryCodeType>(code, out result))
            {
                return result;
            }

            return CountryCodeType.CUSTOMCODE;
        }

        /// <summary>
        /// Gets the US or Canadian state name by state code.
        /// </summary>
        /// <param name="stateCode">The state code.</param>
        /// <returns>The state name.</returns>
        public static string GetStateName(string stateCode)
        {
            if (string.IsNullOrEmpty(stateCode))
            {
                return string.Empty;
            }

            var state = _canadianUSStates.FirstOrDefault(s => s.Code.Equals(stateCode, StringComparison.OrdinalIgnoreCase));
            return state != null ? state.Name : stateCode;
        }

        /// <summary>
        /// Gets the US or Canadian state code by name.
        /// </summary>
        /// <param name="stateName">The state name.</param>
        /// <returns>The state code.</returns>
        public static string GetStateCode(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                return string.Empty;
            }

            var state = _canadianUSStates.FirstOrDefault(s => s.Name.Equals(stateName, StringComparison.OrdinalIgnoreCase));
            return state != null ? state.Code : stateName;
        }

        private static IList<State> GetStates()
        {
            var stateLines = GetAllLines("PaymentProviders.PayPal.CanadianOrUSstates.txt");
            var states = new List<State>();

            for (var i = 0; i < stateLines.Length - 2; i += 2)
            {
                var name = stateLines[i];
                var code = stateLines[i + 1];
                states.Add(new State(code, name));
            }

            return states;
        }

        private static IList<Country> GetCountryCodes()
        {
            var countryCodeLines = GetAllLines("PaymentProviders.PayPal.ISOCodes.txt");
            var countryCodes = new List<Country>();

            foreach (var line in countryCodeLines)
            {
                var values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var alpha3 = values[0];
                var alpha2 = values[1];
                var name = string.Empty;
                for (var i = 2; i < values.Length; i++)
                {
                    name += " " + values[i];
                }
                countryCodes.Add(new Country(alpha2, alpha3, name.TrimStart(' ')));
            }

            return countryCodes;
        }

        private static string[] GetAllLines(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var result = string.Empty;

            using (var stream = assembly.GetManifestResourceStream(resourceFileName))
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