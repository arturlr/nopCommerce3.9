using System.Text;
using System.Text.Json;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public class HttpProductReviewAdapter
{
    private readonly HttpClient _httpClient;
    private readonly bool _useDotNet8Api;

    public HttpProductReviewAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _useDotNet8Api = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
    }

    public async Task<ProductReview> SubmitReviewAsync(int productId, int customerId, string title, string reviewText, int rating)
    {
        if (!_useDotNet8Api)
        {
            throw new NotImplementedException("Legacy review submission not implemented");
        }

        try
        {
            var request = new
            {
                CustomerId = customerId,
                Title = title,
                ReviewText = reviewText,
                Rating = rating
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/v1/products/{productId}/reviews", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var reviewDto = JsonSerializer.Deserialize<ProductReviewDto>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new ProductReview
                {
                    Id = reviewDto.Id,
                    ProductId = reviewDto.ProductId,
                    CustomerId = reviewDto.CustomerId,
                    Title = reviewDto.Title,
                    ReviewText = reviewDto.ReviewText,
                    Rating = reviewDto.Rating,
                    IsApproved = reviewDto.IsApproved,
                    CreatedOnUtc = reviewDto.CreatedOnUtc
                };
            }

            throw new Exception($"API call failed with status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to submit review via .NET 8 API: {ex.Message}", ex);
        }
    }

    public async Task<IList<ProductReview>> GetProductReviewsAsync(int productId, int pageIndex = 0, int pageSize = 10)
    {
        if (!_useDotNet8Api)
        {
            throw new NotImplementedException("Legacy review retrieval not implemented");
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/products/{productId}/reviews?pageNumber={pageIndex + 1}&pageSize={pageSize}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var reviewsDto = JsonSerializer.Deserialize<ProductReviewsDto>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return reviewsDto.Reviews.Select(r => new ProductReview
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    CustomerId = r.CustomerId,
                    Title = r.Title,
                    ReviewText = r.ReviewText,
                    Rating = r.Rating,
                    IsApproved = r.IsApproved,
                    CreatedOnUtc = r.CreatedOnUtc
                }).ToList();
            }

            return new List<ProductReview>();
        }
        catch (Exception ex)
        {
            // Fallback to empty list on error
            return new List<ProductReview>();
        }
    }

    private class ProductReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int CustomerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ReviewText { get; set; } = string.Empty;
        public int Rating { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }

    private class ProductReviewsDto
    {
        public int ProductId { get; set; }
        public ProductReviewDto[] Reviews { get; set; } = Array.Empty<ProductReviewDto>();
    }
}