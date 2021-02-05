using Newtonsoft.Json;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR.Models
{
    /// <summary>
    /// This class is intended to be used internally by EPiServer. We do not support any backward
    /// compatibility on this. Please DO NOT use this in your project.
    /// 
    /// Represents the iframe config model of TokenEx service.
    /// </summary>
    public class TokenExIframeConfigModel
    {
        /// <summary>
        /// Gets or sets the TokenEx id.
        /// </summary>
        [JsonProperty("tokenExID")]
        public string TokenExID { get; set; }

        /// <summary>
        /// Gets or sets the authentication key.
        /// </summary>
        /// <remarks>
        /// Generating a Base64-encoded Hash-based Message Authentication Code (HMAC) based on two things: 
        /// a Client Secret Key and a pipe-delimited concatenation of the following fields:
        /// tokenizationId|origin|timeStamp|tokenScheme
        /// </remarks>
        [JsonProperty("authenticationKey")]
        public string AuthenticationKey { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <remarks>
        /// The timestamp when the authentication key was generated, in yyyyMMddHHmmss format.
        /// </remarks>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the token scheme.
        /// </summary>
        /// <remarks>
        /// The token scheme that is used to generate authentication key.
        /// </remarks>
        [JsonProperty("tokenScheme")]
        public string TokenScheme { get; set; }
    }
}