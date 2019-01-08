using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace EPiServer.Business.Commerce.Payment.DIBS
{
    /// <summary>
    /// This action result posts the specified data dictionary to the specified url, posted data values will be converted using the default string conversion.
    /// </summary>
    public class RedirectAndPostActionResult : ActionResult
    {
        /// <summary>
        /// Action URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Post data.
        /// </summary>
        public Dictionary<string, object> PostData { get; set; }

        public RedirectAndPostActionResult(string url, Dictionary<string, object> postData)
        {
            Url = url;
            PostData = postData ?? new Dictionary<string, object>();
        }

        /// <inheritdoc />
        public override void ExecuteResult(ControllerContext context)
        {
            var html = BuildPostForm(Url, PostData);
            context.HttpContext.Response.Write(html);
        }

        private string BuildPostForm(string url, Dictionary<string, object> postData)
        {
            var formId = "__formRequest";

            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"<form id=\"{formId}\" name=\"{formId}\" action=\"{url}\" method=\"POST\">");

            foreach (var keyPair in postData)
            {
                stringBuilder.Append($"<input type=\"hidden\" name=\"{keyPair.Key}\" value=\"{keyPair.Value}\"/>");
            }

            stringBuilder.Append("</form>");
            stringBuilder.Append($"<script language=\"javascript\">document.{formId}.submit();</script>");

            return stringBuilder.ToString();
        }
    }
}