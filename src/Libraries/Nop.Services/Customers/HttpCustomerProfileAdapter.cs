using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Services.Common;

namespace Nop.Services.Customers
{
    /// <summary>
    /// HTTP adapter for customer profile updates using .NET 8 API
    /// </summary>
    public class HttpCustomerProfileAdapter : ICustomerService
    {
        private readonly ICustomerService _fallbackService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly HttpClient _httpClient;
        private readonly bool _useDotNet8Api;

        public HttpCustomerProfileAdapter(ICustomerService fallbackService, IGenericAttributeService genericAttributeService)
        {
            _fallbackService = fallbackService;
            _genericAttributeService = genericAttributeService;
            _httpClient = new HttpClient();
            _useDotNet8Api = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
        }

        public void UpdateCustomer(Customer customer)
        {
            if (!_useDotNet8Api)
            {
                _fallbackService.UpdateCustomer(customer);
                return;
            }

            try
            {
                UpdateCustomerAsync(customer).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Log error and fallback to legacy implementation
                System.Diagnostics.Debug.WriteLine($"HttpCustomerProfileAdapter.UpdateCustomer failed: {ex.Message}");
                _fallbackService.UpdateCustomer(customer);
            }
        }

        private async Task UpdateCustomerAsync(Customer customer)
        {
            // Get current FirstName and LastName from generic attributes
            var firstName = _genericAttributeService.GetAttribute<string>(customer, "FirstName") ?? "";
            var lastName = _genericAttributeService.GetAttribute<string>(customer, "LastName") ?? "";

            var updateDto = new
            {
                Email = customer.Email,
                FirstName = firstName,
                LastName = lastName
            };

            var json = JsonConvert.SerializeObject(updateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"http://localhost:5000/api/v1/customers/{customer.Id}", content);

            if (response.IsSuccessStatusCode)
            {
                // Update successful - the .NET 8 API has handled the update
                return;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Customer not found in .NET 8 API, fallback to legacy
                throw new Exception("Customer not found in .NET 8 API");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // Email conflict - let the exception bubble up
                var errorJson = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponseDto>(errorJson);
                throw new Exception(string.Join(", ", errorResponse.Errors));
            }
            else
            {
                throw new Exception($"API call failed with status: {response.StatusCode}");
            }
        }

        // Delegate all other methods to fallback service
        public System.Collections.Generic.IPagedList<Customer> GetAllCustomers(DateTime? createdFromUtc = null, DateTime? createdToUtc = null, int affiliateId = 0, int vendorId = 0, int[] customerRoleIds = null, string email = null, string username = null, string firstName = null, string lastName = null, int dayOfBirth = 0, int monthOfBirth = 0, string company = null, string phone = null, string zipPostalCode = null, string ipAddress = null, bool loadOnlyWithShoppingCart = false, Nop.Core.Domain.Orders.ShoppingCartType? sct = null, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            return _fallbackService.GetAllCustomers(createdFromUtc, createdToUtc, affiliateId, vendorId, customerRoleIds, email, username, firstName, lastName, dayOfBirth, monthOfBirth, company, phone, zipPostalCode, ipAddress, loadOnlyWithShoppingCart, sct, pageIndex, pageSize);
        }

        public System.Collections.Generic.IPagedList<Customer> GetOnlineCustomers(DateTime lastActivityFromUtc, int[] customerRoleIds, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            return _fallbackService.GetOnlineCustomers(lastActivityFromUtc, customerRoleIds, pageIndex, pageSize);
        }

        public void DeleteCustomer(Customer customer)
        {
            _fallbackService.DeleteCustomer(customer);
        }

        public Customer GetCustomerById(int customerId)
        {
            return _fallbackService.GetCustomerById(customerId);
        }

        public System.Collections.Generic.IList<Customer> GetCustomersByIds(int[] customerIds)
        {
            return _fallbackService.GetCustomersByIds(customerIds);
        }

        public Customer GetCustomerByGuid(Guid customerGuid)
        {
            return _fallbackService.GetCustomerByGuid(customerGuid);
        }

        public Customer GetCustomerByEmail(string email)
        {
            return _fallbackService.GetCustomerByEmail(email);
        }

        public Customer GetCustomerBySystemName(string systemName)
        {
            return _fallbackService.GetCustomerBySystemName(systemName);
        }

        public Customer GetCustomerByUsername(string username)
        {
            return _fallbackService.GetCustomerByUsername(username);
        }

        public Customer InsertGuestCustomer()
        {
            return _fallbackService.InsertGuestCustomer();
        }

        public void InsertCustomer(Customer customer)
        {
            _fallbackService.InsertCustomer(customer);
        }

        public void ResetCheckoutData(Customer customer, int storeId, bool clearCouponCodes = false, bool clearCheckoutAttributes = false, bool clearRewardPoints = true, bool clearShippingMethod = true, bool clearPaymentMethod = true)
        {
            _fallbackService.ResetCheckoutData(customer, storeId, clearCouponCodes, clearCheckoutAttributes, clearRewardPoints, clearShippingMethod, clearPaymentMethod);
        }

        public int DeleteGuestCustomers(DateTime? createdFromUtc, DateTime? createdToUtc, bool onlyWithoutShoppingCart)
        {
            return _fallbackService.DeleteGuestCustomers(createdFromUtc, createdToUtc, onlyWithoutShoppingCart);
        }

        public void DeleteCustomerRole(CustomerRole customerRole)
        {
            _fallbackService.DeleteCustomerRole(customerRole);
        }

        public CustomerRole GetCustomerRoleById(int customerRoleId)
        {
            return _fallbackService.GetCustomerRoleById(customerRoleId);
        }

        public CustomerRole GetCustomerRoleBySystemName(string systemName)
        {
            return _fallbackService.GetCustomerRoleBySystemName(systemName);
        }

        public System.Collections.Generic.IList<CustomerRole> GetAllCustomerRoles(bool showHidden = false)
        {
            return _fallbackService.GetAllCustomerRoles(showHidden);
        }

        public void InsertCustomerRole(CustomerRole customerRole)
        {
            _fallbackService.InsertCustomerRole(customerRole);
        }

        public void UpdateCustomerRole(CustomerRole customerRole)
        {
            _fallbackService.UpdateCustomerRole(customerRole);
        }

        public System.Collections.Generic.IList<CustomerPassword> GetCustomerPasswords(int? customerId = null, PasswordFormat? passwordFormat = null, int? passwordsToReturn = null)
        {
            return _fallbackService.GetCustomerPasswords(customerId, passwordFormat, passwordsToReturn);
        }

        public CustomerPassword GetCurrentPassword(int customerId)
        {
            return _fallbackService.GetCurrentPassword(customerId);
        }

        public void InsertCustomerPassword(CustomerPassword customerPassword)
        {
            _fallbackService.InsertCustomerPassword(customerPassword);
        }

        public void UpdateCustomerPassword(CustomerPassword customerPassword)
        {
            _fallbackService.UpdateCustomerPassword(customerPassword);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}