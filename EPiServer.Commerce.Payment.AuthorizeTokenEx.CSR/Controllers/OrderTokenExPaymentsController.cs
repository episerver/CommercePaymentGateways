using EPiServer.Commerce.Order;
using EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR.Models;
using EPiServer.Commerce.UI.CustomerService.Routing;
using System.Web.Http;

namespace EPiServer.Commerce.Payment.AuthorizeTokenEx.CSR.Controllers
{
    [EpiRoutePrefix("orders")]
    public class OrderTokenExPaymentsController : TokenExPaymentsControllerBase<IPurchaseOrder>
    {
        [HttpPost]
        [EpiRoute("{orderGroupId}/forms/{formId}/tokenexpayments")]
        public override IHttpActionResult AddPayment(int orderGroupId, int formId, [FromBody] TokenizationPaymentModel model)
        {
            return base.AddPayment(orderGroupId, formId, model);
        }
    }
}