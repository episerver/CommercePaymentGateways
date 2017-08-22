using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace PaymentProviders.DIBS
{
    [ContentType(GUID = "afa29655-74ce-45b8-abe7-696462a6efde",
        DisplayName = "DIBS Payment Page",
        Description = "",
        GroupName = "Payment",
        Order = 100)]
    [ImageUrl("~/styles/images/DIBS-logo.jpg")]
    public class DIBSPage : PageData
    {
    }
}