using Mediachase.Commerce;
using System.Collections.Generic;

namespace PaymentProviders.DIBS
{
    /// <summary>
    /// Handles DIBS supported currencies.
    /// </summary>
    public class DIBSCurrencies
    {
        // Refer to: http://tech.dibspayment.com/D2/Toolbox/Currency_codes
        static readonly IDictionary<string, string> _currencyCodes = new Dictionary<string, string>() {
            { "DKK", "208" }, { "EUR", "978" }, { "USD", "840" }, { "GBP", "826" }, { "SEK", "752" }, { "AUD", "036" }, { "CAD", "124" },
            { "ISK", "352" }, { "JPY", "392" }, { "NZD", "554" }, { "NOK", "578" }, { "CHF", "756" }, { "TRY", "949" } };


        /// <summary>
        /// Converts the currency code of the site to
        /// the ISO4217 number for that currency for DIBS to understand.
        /// </summary>
        /// <param name="currency">The currency.</param>
        /// <returns>The currency code.</returns>
        public static string GetCurrencyCode(Currency currency)
        {
            string code;
            return _currencyCodes.TryGetValue(currency.CurrencyCode, out code) ? code : string.Empty;
        }
    }
}
