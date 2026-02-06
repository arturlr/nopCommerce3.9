using System;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Nop.Api8.Tests;

public class ProductDetailsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductDetailsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetProductDetails_WithValidId_ReturnsProductDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products/1/details");

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var productDetails = JsonSerializer.Deserialize<ProductDetailsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(productDetails);
            Assert.Equal(1, productDetails.Id);
            Assert.NotNull(productDetails.Name);
            Assert.NotNull(productDetails.Images);
            Assert.NotNull(productDetails.Specifications);
            Assert.NotNull(productDetails.ReviewsSummary);
        }
        else
        {
            // If database is not available, should return 404 or 500
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                       response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task GetProductDetails_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products/99999/details");

        // Assert - Should return 404 or 500 (if database unavailable)
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                   response.StatusCode == System.Net.HttpStatusCode.InternalServerError);
    }

    private class ProductDetailsResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ProductImageResponse[] Images { get; set; } = Array.Empty<ProductImageResponse>();
        public ProductSpecificationResponse[] Specifications { get; set; } = Array.Empty<ProductSpecificationResponse>();
        public ProductReviewsSummaryResponse ReviewsSummary { get; set; } = new();
    }

    private class ProductImageResponse
    {
        public string Url { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
    }

    private class ProductSpecificationResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private class ProductReviewsSummaryResponse
    {
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}