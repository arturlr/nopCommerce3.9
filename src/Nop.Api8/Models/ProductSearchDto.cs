namespace Nop.Api8.Models;

public class ProductSearchDto
{
    public ProductDto[] Products { get; set; } = Array.Empty<ProductDto>();
    public PaginationDto Pagination { get; set; } = new();
    public string SearchQuery { get; set; } = string.Empty;
    public SearchFiltersDto AppliedFilters { get; set; } = new();
}

public class SearchFiltersDto
{
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}