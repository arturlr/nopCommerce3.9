# Slice 2: Category Products API Design

## Overview
Design for `GET /api/v1/categories/{id}/products` endpoint with pagination and basic filtering.

## API Endpoint

### Route
```
GET /api/v1/categories/{id}/products
```

### Query Parameters
- `pageNumber` (int, default: 1) - Page number (1-based)
- `pageSize` (int, default: 6) - Items per page (max: 50)
- `orderBy` (string, default: "position") - Sort order: position, name, price, created
- `priceMin` (decimal, optional) - Minimum price filter
- `priceMax` (decimal, optional) - Maximum price filter
- `featuredOnly` (bool, default: false) - Show only featured products

### Response Format
```json
{
  "categoryId": 1,
  "categoryName": "Electronics",
  "products": [
    {
      "id": 1,
      "name": "Product Name",
      "shortDescription": "Description",
      "sku": "SKU123",
      "price": 99.99,
      "isFeatured": true
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 6,
    "totalItems": 25,
    "totalPages": 5,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

## DTOs

### CategoryProductsDto
```csharp
public class CategoryProductsDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<ProductDto> Products { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}
```

### Enhanced ProductDto
```csharp
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsFeatured { get; set; }
}
```

### PaginationMetadata
```csharp
public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}
```

## Implementation Strategy

### Database Query
- Join Category and Product tables via ProductCategory mapping
- Apply filters (price range, featured flag)
- Apply sorting (position, name, price, created date)
- Apply pagination with Skip/Take

### Error Handling
- 404 if category not found
- 400 for invalid query parameters
- 500 for database errors

### Performance Considerations
- Index on ProductCategory.CategoryId
- Index on Product.Price for price filtering
- Limit pageSize to maximum 50 items

## Deferred Features
- Subcategory inclusion
- Advanced filtering (specifications)
- Localization
- ACL (Access Control Lists)
- Advanced caching
- Product images/thumbnails