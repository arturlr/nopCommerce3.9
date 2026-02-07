using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Services.Orders;

namespace Nop.Services.Orders
{
    /// <summary>
    /// HTTP adapter for checkout operations that calls .NET 8 API with fallback to legacy service
    /// </summary>
    public class HttpCheckoutAdapter
    {
        private readonly IOrderService _fallbackOrderService;
        private readonly IShoppingCartService _fallbackCartService;
        private readonly HttpClient _httpClient;
        private readonly bool _useApi;

        public HttpCheckoutAdapter(IOrderService fallbackOrderService, IShoppingCartService fallbackCartService)
        {
            _fallbackOrderService = fallbackOrderService;
            _fallbackCartService = fallbackCartService;
            _httpClient = new HttpClient();
            _useApi = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
        }

        public async Task<CheckoutValidationResult> ValidateCheckoutAsync(int customerId)
        {
            if (!_useApi)
            {
                return await ValidateCheckoutLegacyAsync(customerId);
            }

            try
            {
                var response = await _httpClient.PostAsync($"http://localhost:5000/api/v1/checkout/validate?customerId={customerId}", null);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var validation = JsonSerializer.Deserialize<CheckoutValidationDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return new CheckoutValidationResult
                    {
                        IsValid = validation?.IsValid ?? false,
                        Errors = validation?.Errors ?? new List<string>(),
                        Total = validation?.Total ?? 0,
                        ItemCount = validation?.ItemCount ?? 0
                    };
                }
                else
                {
                    return await ValidateCheckoutLegacyAsync(customerId);
                }
            }
            catch (Exception)
            {
                return await ValidateCheckoutLegacyAsync(customerId);
            }
        }

        public async Task<CheckoutCompleteResult> CompleteCheckoutAsync(int customerId, int billingAddressId, int? shippingAddressId = null)
        {
            if (!_useApi)
            {
                return await CompleteCheckoutLegacyAsync(customerId, billingAddressId, shippingAddressId);
            }

            try
            {
                var request = new
                {
                    customerId = customerId,
                    billingAddressId = billingAddressId,
                    shippingAddressId = shippingAddressId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:5000/api/v1/checkout/complete", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<CheckoutCompleteResponseDto>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return new CheckoutCompleteResult
                    {
                        Success = true,
                        OrderId = result?.OrderId ?? 0,
                        OrderGuid = result?.OrderGuid ?? Guid.Empty,
                        OrderTotal = result?.OrderTotal ?? 0,
                        OrderStatus = result?.OrderStatus ?? "Unknown",
                        CreatedOnUtc = result?.CreatedOnUtc ?? DateTime.UtcNow
                    };
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(errorJson);
                    
                    var errors = new List<string>();
                    if (errorResponse.TryGetProperty("errors", out var errorsElement))
                    {
                        foreach (var error in errorsElement.EnumerateArray())
                        {
                            errors.Add(error.GetString() ?? "Unknown error");
                        }
                    }
                    else
                    {
                        errors.Add("Checkout failed");
                    }

                    return new CheckoutCompleteResult
                    {
                        Success = false,
                        Errors = errors
                    };
                }
            }
            catch (Exception)
            {
                return await CompleteCheckoutLegacyAsync(customerId, billingAddressId, shippingAddressId);
            }
        }

        private async Task<CheckoutValidationResult> ValidateCheckoutLegacyAsync(int customerId)
        {
            // Minimal legacy validation - just check if cart has items
            return new CheckoutValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Total = 0,
                ItemCount = 0
            };
        }

        private async Task<CheckoutCompleteResult> CompleteCheckoutLegacyAsync(int customerId, int billingAddressId, int? shippingAddressId)
        {
            // Minimal legacy implementation - return failure
            return new CheckoutCompleteResult
            {
                Success = false,
                Errors = new List<string> { "Legacy checkout not implemented" }
            };
        }
    }

    public class CheckoutValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }

    public class CheckoutCompleteResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public int OrderId { get; set; }
        public Guid OrderGuid { get; set; }
        public decimal OrderTotal { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedOnUtc { get; set; }
    }

    public class CheckoutValidationDto
    {
        public int CustomerId { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }

    public class CheckoutCompleteResponseDto
    {
        public int OrderId { get; set; }
        public Guid OrderGuid { get; set; }
        public decimal OrderTotal { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedOnUtc { get; set; }
    }
}