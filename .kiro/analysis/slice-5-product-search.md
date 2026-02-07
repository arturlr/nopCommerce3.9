# Slice 5: Product Search Analysis

## Current Implementation

### CatalogController.Search
- **Location**: `src/Presentation/Nop.Web/Controllers/CatalogController.cs`
- **Method**: `Search(SearchModel model, CatalogPagingFilteringModel command)`
- **Key Logic**:
  - Uses `_catalogModelFactory.PrepareSearchModel()` to build search results
  - Supports query string (q), category (cid), manufacturer (mid), vendor (vid), price range (pf/pt)
  - Includes advanced search options (sid for search descriptions)

### ProductService.SearchProducts
- **Location**: `src/Libraries/Nop.Services/Catalog/ProductService.cs`
- **Key Parameters**:
  - `keywords`: Search term
  - `searchDescriptions`: Search in product descriptions
  - `priceMin/priceMax`: Price filtering
  - `categoryIds`: Category filtering
  - `pageIndex/pageSize`: Pagination
- **Search Logic**:
  - Uses stored procedures when available for performance
  - Supports full-text search or LIKE queries
  - Searches Name, ShortDescription, FullDescription fields
  - Includes ACL and store mapping checks

### SearchModel Properties
- `q`: Query string
- `cid`: Category ID
- `pf/pt`: Price from/to
- `sid`: Search in descriptions flag
- Pagination via `CatalogPagingFilteringModel`

## Migration Strategy

### .NET 8 API Design
- **Endpoint**: `GET /api/v1/products/search`
- **Query Parameters**:
  - `q`: Search query (string)
  - `categoryId`: Category filter (int, optional)
  - `minPrice`: Minimum price (decimal, optional)
  - `maxPrice`: Maximum price (decimal, optional)
  - `pageNumber`: Page number (int, default 1)
  - `pageSize`: Page size (int, default 10)

### Implementation Approach
1. Create `ProductSearchDto` for response
2. Add search endpoint to `Program.cs`
3. Implement LIKE-based search on Name, ShortDescription, FullDescription
4. Add pagination and basic filtering
5. Create `HttpCatalogAdapter.SearchProducts` method
6. Add integration tests

### Minimal Scope
- Basic text search only (no full-text search initially)
- Simple LIKE queries on product fields
- Basic price and category filtering
- Standard pagination
- Defer: Advanced filtering, sorting options, manufacturer/vendor filters