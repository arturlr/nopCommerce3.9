using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;

namespace Nop.Services.Customers
{
    /// <summary>
    /// HTTP adapter for customer registration using .NET 8 API
    /// </summary>
    public class HttpCustomerAdapter : ICustomerRegistrationService
    {
        private readonly ICustomerRegistrationService _fallbackService;
        private readonly HttpClient _httpClient;
        private readonly bool _useDotNet8Api;

        public HttpCustomerAdapter(ICustomerRegistrationService fallbackService)
        {
            _fallbackService = fallbackService;
            _httpClient = new HttpClient();
            _useDotNet8Api = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
        }

        public CustomerRegistrationResult RegisterCustomer(CustomerRegistrationRequest request)
        {
            if (!_useDotNet8Api)
            {
                return _fallbackService.RegisterCustomer(request);
            }

            try
            {
                return RegisterCustomerAsync(request).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Log error and fallback to legacy implementation
                System.Diagnostics.Debug.WriteLine($"HttpCustomerAdapter.RegisterCustomer failed: {ex.Message}");
                return _fallbackService.RegisterCustomer(request);
            }
        }

        private async Task<CustomerRegistrationResult> RegisterCustomerAsync(CustomerRegistrationRequest request)
        {
            // For minimal implementation, use placeholder values if FirstName/LastName not available
            // In real scenario, these would come from the registration form
            var firstName = "Customer"; // Placeholder
            var lastName = $"User{DateTime.Now.Ticks}"; // Placeholder with unique suffix

            var registrationDto = new
            {
                Email = request.Email,
                Password = request.Password,
                FirstName = firstName,
                LastName = lastName
            };

            var json = JsonConvert.SerializeObject(registrationDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:5000/api/v1/customers/register", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var customerDto = JsonConvert.DeserializeObject<CustomerDto>(responseJson);

                // Update the customer object with the new ID and details
                request.Customer.Id = customerDto.CustomerId;
                request.Customer.Email = customerDto.Email;
                request.Customer.Active = customerDto.IsActive;
                request.Customer.CreatedOnUtc = customerDto.RegistrationDate;

                return new CustomerRegistrationResult();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponseDto>(errorJson);
                
                var result = new CustomerRegistrationResult();
                foreach (var error in errorResponse.Errors)
                {
                    result.AddError(error);
                }
                return result;
            }
            else
            {
                throw new Exception($"API call failed with status: {response.StatusCode}");
            }
        }

        // Delegate other methods to fallback service
        public CustomerLoginResults ValidateCustomer(string usernameOrEmail, string password)
        {
            return _fallbackService.ValidateCustomer(usernameOrEmail, password);
        }

        public ChangePasswordResult ChangePassword(ChangePasswordRequest request)
        {
            return _fallbackService.ChangePassword(request);
        }

        public void SetEmail(Customer customer, string newEmail, bool requireValidation)
        {
            _fallbackService.SetEmail(customer, newEmail, requireValidation);
        }

        public void SetUsername(Customer customer, string newUsername)
        {
            _fallbackService.SetUsername(customer, newUsername);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // DTOs for API communication
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class ErrorResponseDto
    {
        public List<string> Errors { get; set; } = new List<string>();
    }
}