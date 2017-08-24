using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    [ContentType(GUID = "ce73e4bb-614f-4160-8bbc-be6b3af35f64",
        DisplayName = "DataCash Payment Page",
        Description = "DataCash Payment process page.",
        GroupName = "Payment",
        Order = 100)]
    [ImageUrl("~/styles/images/DataCash-logo.jpg")]
    public class DataCashPage : PageData
    {
    }
}