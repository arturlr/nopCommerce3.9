using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Common;
using Nop.Services.Configuration;

namespace Nop.Services.Customers
{
    /// <summary>
    /// HTTP adapter for admin customer operations using .NET 8 API
    /// </summary>
    public class HttpAdminCustomerAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingService _settingService;
        private readonly ICustomerService _fallbackService;
        private readonly IGenericAttributeService _genericAttributeService;

        public HttpAdminCustomerAdapter(
            HttpClient httpClient,
            ISettingService settingService,
            ICustomerService fallbackService,
            IGenericAttributeService genericAttributeService)
        {
            _httpClient = httpClient;
            _settingService = settingService;
            _fallbackService = fallbackService;
            _genericAttributeService = genericAttributeService;
        }

        public async Task<(IList<Customer> customers, int totalCount)> SearchCustomersAsync(
            string searchTerm = null, int pageIndex = 0, int pageSize = 20)
        {
            var useDotNet8 = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
            if (!useDotNet8)
            {
                return await FallbackSearchCustomers(searchTerm, pageIndex, pageSize);
            }

            try
            {
                var url = $"http://localhost:5000/api/v1/admin/customers?pageNumber={pageIndex + 1}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    url += $"&search={Uri.EscapeDataString(searchTerm)}";
                }

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return await FallbackSearchCustomers(searchTerm, pageIndex, pageSize);
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AdminCustomerListResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var customers = result.Customers.Select(dto => new Customer
                {
                    Id = dto.Id,
                    Email = dto.Email,
                    Active = dto.Active,
                    CreatedOnUtc = dto.CreatedOnUtc,
                    LastActivityDateUtc = dto.LastActivityDateUtc
                }).ToList();

                return (customers, result.TotalCount);
            }
            catch
            {
                return await FallbackSearchCustomers(searchTerm, pageIndex, pageSize);
            }
        }

        public async Task<bool> UpdateCustomerAsync(int customerId, string email, string firstName, string lastName, bool active)
        {
            var useDotNet8 = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
            if (!useDotNet8)
            {
                return await FallbackUpdateCustomer(customerId, email, firstName, lastName, active);
            }

            try
            {
                var request = new
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Active = active
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"http://localhost:5000/api/v1/admin/customers/{customerId}", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return await FallbackUpdateCustomer(customerId, email, firstName, lastName, active);
            }
        }

        public async Task<bool> SoftDeleteCustomerAsync(int customerId)
        {
            var useDotNet8 = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
            if (!useDotNet8)
            {
                return await FallbackSoftDeleteCustomer(customerId);
            }

            try
            {
                var response = await _httpClient.DeleteAsync($"http://localhost:5000/api/v1/admin/customers/{customerId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return await FallbackSoftDeleteCustomer(customerId);
            }
        }

        private async Task<(IList<Customer> customers, int totalCount)> FallbackSearchCustomers(
            string searchTerm, int pageIndex, int pageSize)
        {
            var query = _fallbackService.GetAllCustomers(
                customerRoleIds: null,
                email: searchTerm,
                username: null,
                firstName: null,
                lastName: null,
                dayOfBirth: 0,
                monthOfBirth: 0,
                company: null,
                phone: null,
                zipPostalCode: null,
                loadOnlyWithShoppingCart: false,
                sct: null,
                pageIndex: pageIndex,
                pageSize: pageSize);

            return (query.ToList(), query.TotalCount);
        }

        private async Task<bool> FallbackUpdateCustomer(int customerId, string email, string firstName, string lastName, bool active)
        {
            try
            {
                var customer = _fallbackService.GetCustomerById(customerId);
                if (customer == null) return false;

                customer.Email = email;
                customer.Active = active;
                _fallbackService.UpdateCustomer(customer);

                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.FirstName, firstName);
                _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.LastName, lastName);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> FallbackSoftDeleteCustomer(int customerId)
        {
            try
            {
                var customer = _fallbackService.GetCustomerById(customerId);
                if (customer == null) return false;

                customer.Active = false;
                _fallbackService.UpdateCustomer(customer);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class AdminCustomerListResponse
    {
        public List<AdminCustomerResponse> Customers { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class AdminCustomerResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime LastActivityDateUtc { get; set; }
    }
}