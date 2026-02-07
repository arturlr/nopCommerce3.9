namespace Nop.Api8.Models;

public class ProductReviewsDto
{
    public int ProductId { get; set; }
    public ProductReviewDto[] Reviews { get; set; } = Array.Empty<ProductReviewDto>();
    public PaginationMetadata Pagination { get; set; } = new();
}