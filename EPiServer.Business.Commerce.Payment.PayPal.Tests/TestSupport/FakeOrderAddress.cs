using EPiServer.Commerce.Order;
using System.Collections;

namespace EPiServer.Business.Commerce.Payment.PayPal.Tests.TestSupport
{
    class FakeOrderAddress : IOrderAddress
    {
        public string Id { get; set; }

        public string City { get; set; }

        public string CountryCode { get; set; }

        public string CountryName { get; set; }

        public string DaytimePhoneNumber { get; set; }

        public string Email { get; set; }

        public string EveningPhoneNumber { get; set; }

        public string FaxNumber { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Line1 { get; set; }

        public string Line2 { get; set; }

        public string Organization { get; set; }

        public string PostalCode { get; set; }

        public string RegionCode { get; set; }

        public string RegionName { get; set; }

        public Hashtable Properties { get; set; }

        public static FakeOrderAddress CreateOrderAddress(Hashtable properties = null)
        {
            return CreateOrderAddress("JohnDoe", "4122 Any street", "Springfield", "22153", "VA", "USA", properties);
        }

        public static FakeOrderAddress CreateOrderAddress(string name, string line1, string city, string postalCode, string region, string countryCode, Hashtable properties = null)
        {
            return new FakeOrderAddress
            {
                Id = name,
                City = city,
                CountryCode = countryCode,
                CountryName = "",
                DaytimePhoneNumber = "",
                Email = "test@email.com",
                EveningPhoneNumber = "",
                FaxNumber = "",
                FirstName = "John",
                LastName = "Doe",
                Line1 = line1,
                Line2 = "",
                Organization = "",
                PostalCode = postalCode,
                RegionCode = region,
                RegionName = region,
                Properties = properties ?? new Hashtable()
            };
        }
    }
}
