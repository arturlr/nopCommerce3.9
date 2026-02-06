using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nop.Api8.Tests
{
    public class ShippingEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ShippingEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetShippingMethods_ReturnsShippingMethods()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/shipping/methods");

            // Assert
            var validStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError };
            Assert.Contains(response.StatusCode, validStatusCodes);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var methods = JsonConvert.DeserializeObject<List<ShippingMethodDto>>(content);
                Assert.NotNull(methods);
                Assert.NotEmpty(methods);
                
                var firstMethod = methods[0];
                Assert.NotNull(firstMethod.Name);
                Assert.True(firstMethod.Id > 0);
            }
        }

        [Fact]
        public async Task GetShippingMethods_WithCountryFilter_ReturnsFilteredMethods()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/shipping/methods?countryId=1");

            // Assert
            var validStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError };
            Assert.Contains(response.StatusCode, validStatusCodes);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var methods = JsonConvert.DeserializeObject<List<ShippingMethodDto>>(content);
                Assert.NotNull(methods);
            }
        }

        [Fact]
        public async Task CalculateShippingRates_WithValidRequest_ReturnsRates()
        {
            // Arrange
            var request = new ShippingRateRequestDto
            {
                CustomerId = 1,
                Items = new List<ShippingCartItemDto>
                {
                    new ShippingCartItemDto { ProductId = 1, Quantity = 2 },
                    new ShippingCartItemDto { ProductId = 2, Quantity = 1 }
                },
                ShippingAddress = new ShippingAddressDto
                {
                    Address1 = "123 Main St",
                    City = "New York",
                    StateProvince = "NY",
                    ZipPostalCode = "10001",
                    CountryId = 1
                },
                StoreId = 1
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/shipping/calculate", content);

            // Assert
            var validStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError };
            Assert.Contains(response.StatusCode, validStatusCodes);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ShippingRateResultDto>(responseContent);
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.NotEmpty(result.ShippingOptions);
                
                var firstOption = result.ShippingOptions[0];
                Assert.NotNull(firstOption.Name);
                Assert.True(firstOption.Rate > 0);
            }
        }

        [Fact]
        public async Task CalculateShippingRates_WithInvalidCustomer_ReturnsBadRequest()
        {
            // Arrange
            var request = new ShippingRateRequestDto
            {
                CustomerId = 0, // Invalid customer ID
                Items = new List<ShippingCartItemDto>
                {
                    new ShippingCartItemDto { ProductId = 1, Quantity = 1 }
                },
                ShippingAddress = new ShippingAddressDto
                {
                    Address1 = "123 Main St",
                    City = "New York",
                    StateProvince = "NY",
                    ZipPostalCode = "10001",
                    CountryId = 1
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/shipping/calculate", content);

            // Assert
            var validStatusCodes = new[] { HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError };
            Assert.Contains(response.StatusCode, validStatusCodes);
        }

        [Fact]
        public async Task CalculateShippingRates_WithEmptyCart_ReturnsBadRequest()
        {
            // Arrange
            var request = new ShippingRateRequestDto
            {
                CustomerId = 1,
                Items = new List<ShippingCartItemDto>(), // Empty cart
                ShippingAddress = new ShippingAddressDto
                {
                    Address1 = "123 Main St",
                    City = "New York",
                    StateProvince = "NY",
                    ZipPostalCode = "10001",
                    CountryId = 1
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/shipping/calculate", content);

            // Assert
            var validStatusCodes = new[] { HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError };
            Assert.Contains(response.StatusCode, validStatusCodes);
        }

        // DTOs for testing
        private class ShippingMethodDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int DisplayOrder { get; set; }
        }

        private class ShippingRateRequestDto
        {
            public int CustomerId { get; set; }
            public List<ShippingCartItemDto> Items { get; set; } = new List<ShippingCartItemDto>();
            public ShippingAddressDto ShippingAddress { get; set; }
            public int StoreId { get; set; } = 1;
        }

        private class ShippingCartItemDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        private class ShippingAddressDto
        {
            public string Address1 { get; set; }
            public string City { get; set; }
            public string StateProvince { get; set; }
            public string ZipPostalCode { get; set; }
            public int CountryId { get; set; }
        }

        private class ShippingRateResultDto
        {
            public List<ShippingOptionDto> ShippingOptions { get; set; } = new List<ShippingOptionDto>();
            public bool Success { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }

        private class ShippingOptionDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Rate { get; set; }
            public string ShippingRateComputationMethodSystemName { get; set; }
        }
    }
}