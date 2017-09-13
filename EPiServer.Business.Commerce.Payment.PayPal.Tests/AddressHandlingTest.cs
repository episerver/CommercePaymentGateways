using EPiServer.Business.Commerce.Payment.PayPal.Tests.TestSupport;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Customers;
using PayPal.PayPalAPIInterfaceService.Model;
using Xunit;

namespace EPiServer.Business.Commerce.Payment.PayPal.Tests
{
    public class AddressHandlingTest
    {
        [Fact]
        public void ToAddressType_ShouldConvertCorrectly()
        {
            var orderAddress = CreateOrderAddress();
            var result = AddressHandling.ToAddressType(orderAddress);

            Assert.Equal("Los Angeles", result.CityName);
            Assert.Equal(CountryCodeType.US, result.Country);
            Assert.Equal("FakeAddress", result.Street1);
            Assert.Equal("", result.Street2);
            Assert.Equal("90001", result.PostalCode);
            Assert.Equal("99999999", result.Phone);
            Assert.Equal("John Doe", result.Name);
            Assert.Equal("CA", result.StateOrProvince);
        }

        [Fact]
        public void UpdateOrderAddress_ShouldUpdateCorrectly()
        {
            var orderAddress = CreateOrderAddress();
            var paypalAddress = AddressHandling.ToAddressType(orderAddress);

            paypalAddress.Street1 = "Street1";
            AddressHandling.UpdateOrderAddress(orderAddress, CustomerAddressTypeEnum.Billing, paypalAddress, "test@abc.com");

            Assert.Equal("Street1", orderAddress.Line1);
            Assert.Equal("test@abc.com", orderAddress.Email);
        }
        
        private IOrderAddress CreateOrderAddress()
        {
            var address = FakeOrderAddress.CreateOrderAddress("FakeAddress", "FakeAddress", "Los Angeles", "90001", "California", "USA");
            address.DaytimePhoneNumber = "99999999";
            return address;
        }
    }
}
