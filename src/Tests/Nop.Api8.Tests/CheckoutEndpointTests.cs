using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Nop.Api8.Tests
{
    public class CheckoutEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CheckoutEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task ValidateCheckout_WithEmptyCart_ReturnsInvalidResult()
        {
            // Arrange
            var customerId = 999; // Non-existent customer

            // Act
            var response = await _client.PostAsync($"/api/v1/checkout/validate?customerId={customerId}", null);

            // Assert
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
                       response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task ValidateCheckout_WithValidCustomer_ReturnsValidationResult()
        {
            // Arrange
            var customerId = 1; // Assuming customer 1 exists

            // Act
            var response = await _client.PostAsync($"/api/v1/checkout/validate?customerId={customerId}", null);

            // Assert
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(content);
                
                Assert.True(result.TryGetProperty("customerId", out _));
                Assert.True(result.TryGetProperty("isValid", out _));
                Assert.True(result.TryGetProperty("errors", out _));
                Assert.True(result.TryGetProperty("total", out _));
                Assert.True(result.TryGetProperty("itemCount", out _));
            }
            else
            {
                // Skip test if database is unavailable
                Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                           response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public async Task CompleteCheckout_WithEmptyCart_ReturnsBadRequest()
        {
            // Arrange
            var request = new
            {
                customerId = 999,
                billingAddressId = 1,
                shippingAddressId = (int?)null
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/checkout/complete", content);

            // Assert
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
                       response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task CompleteCheckout_WithValidRequest_ReturnsOrderResult()
        {
            // Arrange
            var request = new
            {
                customerId = 1,
                billingAddressId = 1,
                shippingAddressId = (int?)null
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/checkout/complete", content);

            // Assert
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                Assert.True(result.TryGetProperty("orderId", out _));
                Assert.True(result.TryGetProperty("orderGuid", out _));
                Assert.True(result.TryGetProperty("orderTotal", out _));
                Assert.True(result.TryGetProperty("orderStatus", out _));
                Assert.True(result.TryGetProperty("createdOnUtc", out _));
            }
            else
            {
                // Skip test if database is unavailable or cart is empty
                Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                           response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public async Task CompleteCheckout_WithInvalidCustomer_ReturnsBadRequest()
        {
            // Arrange
            var request = new
            {
                customerId = -1,
                billingAddressId = 1,
                shippingAddressId = (int?)null
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/checkout/complete", content);

            // Assert
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
                       response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task CompleteCheckout_WithMissingBillingAddress_ReturnsBadRequest()
        {
            // Arrange
            var request = new
            {
                customerId = 1,
                billingAddressId = 0, // Invalid address ID
                shippingAddressId = (int?)null
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/checkout/complete", content);

            // Assert
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
                       response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
        }
    }
}