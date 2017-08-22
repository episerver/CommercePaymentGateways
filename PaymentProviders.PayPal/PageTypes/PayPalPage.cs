using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace PaymentProviders.PayPal
{
    [ContentType(GUID = "adb6231d-3f48-4a1c-bfc6-353604a829e3",
        DisplayName = "PayPal Payment Page",
        Description = "PayPal Payment process page.",
        GroupName = "Payment",
        Order = 100)]
    [ImageUrl("~/styles/images/PayPal-logo.jpg")]
    public class PayPalPage : PageData
    {
    }
}