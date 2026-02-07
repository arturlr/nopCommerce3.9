namespace Nop.Api8.Models;

public class ProductDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductImageDto[] Images { get; set; } = Array.Empty<ProductImageDto>();
    public ProductSpecificationDto[] Specifications { get; set; } = Array.Empty<ProductSpecificationDto>();
    public ProductReviewsSummaryDto ReviewsSummary { get; set; } = new();
}

public class ProductImageDto
{
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
}

public class ProductSpecificationDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ProductReviewsSummaryDto
{
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
}