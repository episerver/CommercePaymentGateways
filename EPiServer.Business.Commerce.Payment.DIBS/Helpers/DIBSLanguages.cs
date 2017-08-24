using System.Collections.Generic;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    /// <summary>
    /// Handles DIBS supported languages.
    /// </summary>
    public class DIBSLanguages
    {
        // Refer to lang parameter in http://tech.dibspayment.com/D2/Hosted/Input_parameters/Standard.
        static readonly IDictionary<string, string> _supportedLanguage = new Dictionary<string, string>
        {
            { "Danish", "da" }, { "English", "en" }, { "German", "de" },{ "Spanish", "es" }, { "Finnish", "fi" }, { "Faroese", "fo" }, { "French", "fr" },
            { "Italian", "it" }, { "Dutch", "nl" }, { "Norwegian", "no" },{ "Polish", "pl" }, { "Swedish", "sv" }, { "Greenlandic", "kl" },
        };

        /// <summary>
        /// Converts the site language to the language which DIBS can support.
        /// Refer to: http://tech.dibspayment.com/D2/Hosted/Input_parameters/Standard for more information.
        /// </summary>
        /// <returns>The supported language.</returns>
        public static string GetCurrentDIBSSupportedLanguage(string languageName)
        {
            string lang;
            return _supportedLanguage.TryGetValue(languageName, out lang)? lang: "en";
        }
    }
}
