using EPiServer.Commerce.Order;
using EPiServer.Security;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Customers.Profile;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Security;
using Mediachase.Commerce.Website.Helpers;
using PayPal.PayPalAPIInterfaceService.Model;
using System;
using System.Linq;
using System.Web;

namespace EPiServer.Business.Commerce.Payment.PayPal
{
    /// <summary>
    /// Handles addresses processing.
    /// </summary>
    public static class AddressHandling
    {
        /// <summary>
        /// Gets the PayPal address type model from a specific IOrderAddress.
        /// </summary>
        /// <value>The PayPal address type model.</value>
        public static AddressType ToAddressType(IOrderAddress orderAddress)
        {
            var addressType = new AddressType();
            addressType.CityName = orderAddress.City;
            addressType.Country = CountriesAndStates.GetAlpha2CountryCode(orderAddress.CountryCode);
            addressType.CountryName = orderAddress.CountryName;
            addressType.Street1 = orderAddress.Line1;
            addressType.Street2 = orderAddress.Line2;
            addressType.PostalCode = orderAddress.PostalCode;
            addressType.Phone = (string.IsNullOrEmpty(orderAddress.DaytimePhoneNumber) ? orderAddress.EveningPhoneNumber : orderAddress.DaytimePhoneNumber);
            addressType.Name = orderAddress.FirstName + " " + orderAddress.LastName;

            var stateName = orderAddress.RegionName;
            var address = orderAddress as OrderAddress;
            if (!string.IsNullOrEmpty(address?.State))
            {
                stateName = address.State;
            }

            addressType.StateOrProvince = CountriesAndStates.GetStateCode(stateName);
            return addressType;
        }

        /// <summary>
        /// Updates order address information from PayPal address type model.
        /// </summary>
        /// <param name="orderAddress">The order address.</param>
        /// <param name="customerAddressType">The customer address type.</param>
        /// <param name="addressType">The PayPal address type.</param>
        /// <param name="payerEmail">The PayPal payer email.</param>
        public static void UpdateOrderAddress(IOrderAddress orderAddress, CustomerAddressTypeEnum customerAddressType, AddressType addressType, string payerEmail)
        {
            var name = Utilities.StripPreviewText(addressType.Name.Trim(), 46);

            orderAddress.Id = name;
            orderAddress.City = addressType.CityName;
            orderAddress.CountryCode = CountriesAndStates.GetAlpha3CountryCode(addressType.Country.ToString().ToUpperInvariant());
            orderAddress.DaytimePhoneNumber = addressType.Phone;
            orderAddress.EveningPhoneNumber = addressType.Phone;
            orderAddress.Line1 = addressType.Street1;
            orderAddress.Line2 = addressType.Street2;
            orderAddress.PostalCode = addressType.PostalCode;
            orderAddress.Email = payerEmail;
            var index = name.IndexOf(' ');
            orderAddress.FirstName = index >= 0 ? name.Substring(0, index) : name;
            orderAddress.LastName = index >= 0 ? name.Substring(index + 1) : string.Empty;
            orderAddress.RegionCode = addressType.StateOrProvince;
            orderAddress.RegionName = CountriesAndStates.GetStateName(addressType.StateOrProvince);
            var address = orderAddress as OrderAddress;
            if (address != null)
            {
                address.State = orderAddress.RegionName;
            }

            TrySaveCustomerAddress(orderAddress, customerAddressType);
        }

        /// <summary>
        /// Determines whether the address is changed.
        /// </summary>
        /// <param name="orderAddress">The order address.</param>
        /// <param name="addressType">The PayPal address type.</param>
        /// <returns>
        /// 	<c>true</c> if [is address changed] [the specified order address]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAddressChanged(IOrderAddress orderAddress, AddressType addressType)
        {
            return !string.Equals(orderAddress.City, addressType.CityName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(orderAddress.CountryCode, CountriesAndStates.GetAlpha3CountryCode(addressType.Country.ToString().ToUpperInvariant()), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(orderAddress.Line1, addressType.Street1, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(orderAddress.Line2 ?? string.Empty, addressType.Street2 ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(orderAddress.PostalCode, addressType.PostalCode, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// If current user is registered user, try to save the OrderAddress to its Contact.
        /// </summary>
        /// <param name="orderAddress">The modified order address.</param>
        /// <param name="customerAddressType">The customer address type.</param>
        private static void TrySaveCustomerAddress(IOrderAddress orderAddress, CustomerAddressTypeEnum customerAddressType)
        {
            if (HttpContext.Current == null)
            {
                return;
            }
            var httpProfile = HttpContext.Current.Profile;
            var profile = httpProfile == null ? null : new CustomerProfileWrapper(httpProfile);

            if (profile == null || profile.IsAnonymous)
            {
                return;
            }

            // Add to contact address
            var customerContact = PrincipalInfo.CurrentPrincipal.GetCustomerContact();
            if (customerContact != null)
            {
                var customerAddress = CustomerAddress.CreateForApplication();
                customerAddress.Name = orderAddress.Id;
                customerAddress.AddressType = customerAddressType;
                customerAddress.City = orderAddress.City;
                customerAddress.CountryCode = orderAddress.CountryCode;
                customerAddress.CountryName = orderAddress.CountryName;
                customerAddress.DaytimePhoneNumber = orderAddress.DaytimePhoneNumber;
                customerAddress.Email = orderAddress.Email;
                customerAddress.EveningPhoneNumber = orderAddress.EveningPhoneNumber;
                customerAddress.FirstName = orderAddress.FirstName;
                customerAddress.LastName = orderAddress.LastName;
                customerAddress.Line1 = orderAddress.Line1;
                customerAddress.Line2 = orderAddress.Line2;
                customerAddress.PostalCode = orderAddress.PostalCode;
                customerAddress.RegionName = orderAddress.RegionName;
                customerAddress.RegionCode = orderAddress.RegionCode;

#pragma warning disable 618
                if (customerContact.ContactAddresses == null || !StoreHelper.IsAddressInCollection(customerContact.ContactAddresses, customerAddress))
#pragma warning restore 618
                {
                    // If there is an address has the same name with new address, 
                    // rename new address by appending the index to the name.
                    var addressCount = customerContact.ContactAddresses.Count(a => a.Name == customerAddress.Name);
                    customerAddress.Name = $"{customerAddress.Name}{(addressCount == 0 ? string.Empty : "-" + addressCount.ToString())}";

                    customerContact.AddContactAddress(customerAddress);
                    customerContact.SaveChanges();
                }
            }
        }
    }
}