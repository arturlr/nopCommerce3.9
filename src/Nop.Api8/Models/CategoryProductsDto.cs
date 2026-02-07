namespace Nop.Api8.Models;

public class CategoryProductsDto
{
    public List<ProductDto> Products { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class PaginationMetadata
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}