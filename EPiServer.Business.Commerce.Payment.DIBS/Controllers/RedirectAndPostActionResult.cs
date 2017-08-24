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
            stringBuilder.Append(string.Format("<form id=\"{0}\" name=\"{0}\" action=\"{1}\" method=\"POST\">", formId, url));
            foreach (var item in postData)
            {
                stringBuilder.Append(string.Format("<input type=\"hidden\" name=\"{0}\" value=\"{1}\"/>", item.Key, item.Value));
            }
            stringBuilder.Append("</form>");

            stringBuilder.Append(string.Format("<script language=\"javascript\">document.{0}.submit();</script>", formId));

            return stringBuilder.ToString();
        }
    }
}