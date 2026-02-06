# Slice 3: Product Details Page - API Design

## Endpoint Design

### New Endpoint: GET /api/v1/products/{id}/details

Enhanced product details endpoint with full information.

**Parameters:**
- `id` (int, required) - Product ID

**Response Model: ProductDetailsDto**
```csharp
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
```

## Database Entities

### New Entities Required

**ProductPicture** (junction table)
```csharp
public class ProductPicture
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int PictureId { get; set; }
    public int DisplayOrder { get; set; }
    
    public Product Product { get; set; } = null!;
    public Picture Picture { get; set; } = null!;
}
```

**Picture** (image storage)
```csharp
public class Picture
{
    public int Id { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string SeoFilename { get; set; } = string.Empty;
    public string AltAttribute { get; set; } = string.Empty;
    public string TitleAttribute { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}
```

**ProductSpecificationAttribute** (junction table)
```csharp
public class ProductSpecificationAttribute
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int SpecificationAttributeOptionId { get; set; }
    public string CustomValue { get; set; } = string.Empty;
    public bool AllowFiltering { get; set; }
    public bool ShowOnProductPage { get; set; }
    public int DisplayOrder { get; set; }
    
    public Product Product { get; set; } = null!;
    public SpecificationAttributeOption SpecificationAttributeOption { get; set; } = null!;
}
```

**SpecificationAttributeOption**
```csharp
public class SpecificationAttributeOption
{
    public int Id { get; set; }
    public int SpecificationAttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    
    public SpecificationAttribute SpecificationAttribute { get; set; } = null!;
}
```

**SpecificationAttribute**
```csharp
public class SpecificationAttribute
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
```

**ProductReview**
```csharp
public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReviewText { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    
    public Product Product { get; set; } = null!;
}
```

## DbContext Updates

Add new DbSets and configure relationships:

```csharp
public DbSet<ProductPicture> ProductPictures { get; set; }
public DbSet<Picture> Pictures { get; set; }
public DbSet<ProductSpecificationAttribute> ProductSpecificationAttributes { get; set; }
public DbSet<SpecificationAttributeOption> SpecificationAttributeOptions { get; set; }
public DbSet<SpecificationAttribute> SpecificationAttributes { get; set; }
public DbSet<ProductReview> ProductReviews { get; set; }
```

## API Implementation

### Minimal Endpoint Implementation
```csharp
app.MapGet("/api/v1/products/{id:int}/details", async (int id, NopDbContext db) =>
{
    var product = await db.Products
        .Include(p => p.ProductPictures)
            .ThenInclude(pp => pp.Picture)
        .Include(p => p.ProductSpecificationAttributes)
            .ThenInclude(psa => psa.SpecificationAttributeOption)
                .ThenInclude(sao => sao.SpecificationAttribute)
        .Include(p => p.ProductReviews.Where(pr => pr.IsApproved))
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product == null)
        return Results.NotFound();

    var dto = new ProductDetailsDto
    {
        Id = product.Id,
        Name = product.Name,
        ShortDescription = product.ShortDescription,
        FullDescription = product.FullDescription,
        Sku = product.Sku,
        Price = product.Price,
        Images = product.ProductPictures
            .OrderBy(pp => pp.DisplayOrder)
            .Select(pp => new ProductImageDto
            {
                Url = $"/images/{pp.Picture.Id}/{pp.Picture.SeoFilename}",
                AltText = pp.Picture.AltAttribute
            }).ToArray(),
        Specifications = product.ProductSpecificationAttributes
            .Where(psa => psa.ShowOnProductPage)
            .OrderBy(psa => psa.DisplayOrder)
            .Select(psa => new ProductSpecificationDto
            {
                Name = psa.SpecificationAttributeOption.SpecificationAttribute.Name,
                Value = !string.IsNullOrEmpty(psa.CustomValue) 
                    ? psa.CustomValue 
                    : psa.SpecificationAttributeOption.Name
            }).ToArray(),
        ReviewsSummary = new ProductReviewsSummaryDto
        {
            TotalReviews = product.ProductReviews.Count,
            AverageRating = product.ProductReviews.Any() 
                ? (decimal)product.ProductReviews.Average(pr => pr.Rating) 
                : 0
        }
    };

    return Results.Ok(dto);
});
```

## Adapter Integration

Update HttpCatalogAdapter to support product details:

```csharp
public async Task<ProductDetailsDto?> GetProductDetailsAsync(int productId)
{
    if (!_useNet8Api)
        return null;

    try
    {
        var response = await _httpClient.GetAsync($"/api/v1/products/{productId}/details");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ProductDetailsDto>(json, _jsonOptions);
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to get product details from .NET 8 API for product {ProductId}", productId);
    }

    return null;
}
```

## Response Examples

### Success Response (200 OK)
```json
{
  "id": 1,
  "name": "Sample Product",
  "shortDescription": "A great product",
  "fullDescription": "<p>This is the full description with <strong>HTML</strong> content.</p>",
  "sku": "SAMPLE-001",
  "price": 29.99,
  "images": [
    {
      "url": "/images/1/sample-product.jpg",
      "altText": "Sample Product Image"
    }
  ],
  "specifications": [
    {
      "name": "Color",
      "value": "Red"
    },
    {
      "name": "Size",
      "value": "Large"
    }
  ],
  "reviewsSummary": {
    "averageRating": 4.5,
    "totalReviews": 12
  }
}
```

### Not Found Response (404)
```json
{
  "error": "Product not found"
}
```

## Implementation Notes

1. **Image URLs**: Use simple path-based URLs for now, defer CDN/complex image handling
2. **Specifications**: Only show specifications marked with `ShowOnProductPage = true`
3. **Reviews**: Only count approved reviews for summary
4. **Localization**: Defer localization support to future iterations
5. **Caching**: No caching in initial implementation
6. **Error Handling**: Basic error responses, detailed logging