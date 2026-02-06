# Slice 5: Product Search API Design

## Endpoint Specification

### GET /api/v1/products/search

**Query Parameters:**
- `q` (string, optional): Search query text
- `categoryId` (int, optional): Filter by category ID
- `minPrice` (decimal, optional): Minimum price filter
- `maxPrice` (decimal, optional): Maximum price filter
- `pageNumber` (int, optional, default=1): Page number
- `pageSize` (int, optional, default=10): Items per page

**Response Format:**
```json
{
  "products": [
    {
      "id": 1,
      "name": "Product Name",
      "shortDescription": "Brief description",
      "price": 29.99,
      "categoryId": 5,
      "categoryName": "Electronics"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalItems": 25,
    "totalPages": 3
  },
  "searchQuery": "laptop",
  "appliedFilters": {
    "categoryId": 5,
    "minPrice": 100.00,
    "maxPrice": 500.00
  }
}
```

## Data Model

### ProductSearchDto
```csharp
public class ProductSearchDto
{
    public ProductDto[] Products { get; set; }
    public PaginationDto Pagination { get; set; }
    public string SearchQuery { get; set; }
    public SearchFiltersDto AppliedFilters { get; set; }
}

public class SearchFiltersDto
{
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
```

## Search Logic

### Database Query
- Search in Product.Name, Product.ShortDescription, Product.FullDescription
- Use LIKE queries with wildcards: `%{query}%`
- Apply price range filtering
- Apply category filtering via ProductCategory join
- Include pagination with OFFSET/LIMIT

### Example Query
```sql
SELECT p.Id, p.Name, p.ShortDescription, p.Price, pc.CategoryId, c.Name as CategoryName
FROM Product p
LEFT JOIN Product_Category_Mapping pc ON p.Id = pc.ProductId
LEFT JOIN Category c ON pc.CategoryId = c.Id
WHERE (p.Name LIKE '%query%' OR p.ShortDescription LIKE '%query%' OR p.FullDescription LIKE '%query%')
  AND (@categoryId IS NULL OR pc.CategoryId = @categoryId)
  AND (@minPrice IS NULL OR p.Price >= @minPrice)
  AND (@maxPrice IS NULL OR p.Price <= @maxPrice)
  AND p.Published = 1
  AND p.Deleted = 0
ORDER BY p.Name
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
```

## Integration Points

### HttpCatalogAdapter
- Add `SearchProducts` method
- Call .NET 8 API when feature flag enabled
- Fallback to existing `ProductService.SearchProducts`
- Map between API response and legacy models