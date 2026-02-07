# Slice 2: Category Listing Analysis

## Overview
Analysis of nopCommerce 3.90 category listing functionality for migration to .NET 8.

## Controller Action Analysis

### File: `src/Presentation/Nop.Web/Controllers/CatalogController.cs`
### Method: `Category(int categoryId, CatalogPagingFilteringModel command)`

**Inputs:**
- `categoryId` (int): Category identifier from route
- `command` (CatalogPagingFilteringModel): Pagination, filtering, and sorting parameters

**Business Logic Flow:**
1. **Category Validation:**
   - Load category by ID using `_categoryService.GetCategoryById(categoryId)`
   - Return 404 if category is null or deleted
   - Check published status, ACL permissions, store mapping
   - Allow preview for users with "Manage categories" permission

2. **User Context:**
   - Save "Continue shopping" URL to customer attributes
   - Display edit link for admin users
   - Log activity: "PublicStore.ViewCategory"

3. **Model Preparation:**
   - Delegate to `_catalogModelFactory.PrepareCategoryModel(category, command)`
   - Get template view path using `_catalogModelFactory.PrepareCategoryTemplateViewPath()`

**Dependencies:**
- `ICatalogModelFactory` - Main model preparation
- `ICategoryService` - Category data access
- `IAclService` - Access control
- `IStoreMappingService` - Store mapping
- `IPermissionService` - Permission checks
- `IGenericAttributeService` - Customer attributes
- `ICustomerActivityService` - Activity logging
- `ILocalizationService` - Localization

## Model Factory Analysis

### File: `src/Presentation/Nop.Web/Factories/CatalogModelFactory.cs`
### Method: `PrepareCategoryModel(Category category, CatalogPagingFilteringModel command)`

**Model Building Process:**

1. **Basic Category Data:**
   - ID, Name, Description (localized)
   - Meta tags (Keywords, Description, Title)
   - SEO-friendly name (SeName)

2. **Pagination & Filtering Setup:**
   - Sorting options via `PrepareSortingOptions()`
   - View modes (grid/list) via `PrepareViewModes()`
   - Page size options via `PreparePageSizeOptions()`
   - Price range filters from category configuration
   - Specification attribute filters

3. **Category Breadcrumb:**
   - Built using `category.GetCategoryBreadCrumb()` if enabled
   - Cached with customer roles, store, and language context

4. **Subcategories:**
   - Load via `_categoryService.GetAllCategoriesByParentCategoryId()`
   - Include pictures with caching
   - Cached with multiple context keys

5. **Featured Products:**
   - Load if `!_catalogSettings.IgnoreFeaturedProducts`
   - Use `_productService.SearchProducts()` with `featuredProducts: true`
   - Cache existence check for performance
   - Convert to `ProductOverviewModel` via `_productModelFactory`

6. **Regular Products:**
   - Include subcategory products if `_catalogSettings.ShowProductsFromSubcategories`
   - Apply filters: price range, specification attributes
   - Exclude featured products if `!_catalogSettings.IncludeFeaturedProductsInNormalLists`
   - Support sorting via `ProductSortingEnum`
   - Paginated results
   - Convert to `ProductOverviewModel`

7. **Specification Filters:**
   - Build filterable specification options
   - Support multi-select filtering

## View Models

### CategoryModel Structure
```csharp
public class CategoryModel : BaseNopEntityModel
{
    // Basic Info
    public string Name { get; set; }
    public string Description { get; set; }
    public string MetaKeywords/MetaDescription/MetaTitle { get; set; }
    public string SeName { get; set; }
    public PictureModel PictureModel { get; set; }
    
    // Navigation
    public bool DisplayCategoryBreadcrumb { get; set; }
    public IList<CategoryModel> CategoryBreadcrumb { get; set; }
    public IList<SubCategoryModel> SubCategories { get; set; }
    
    // Products
    public IList<ProductOverviewModel> FeaturedProducts { get; set; }
    public IList<ProductOverviewModel> Products { get; set; }
    
    // Pagination & Filtering
    public CatalogPagingFilteringModel PagingFilteringContext { get; set; }
}
```

### CatalogPagingFilteringModel Features
- **Sorting:** Product sorting options (price, name, creation date, etc.)
- **View Modes:** Grid vs List display
- **Page Size:** Configurable page sizes
- **Price Filtering:** Range-based price filters
- **Specification Filtering:** Multi-attribute filtering
- **Pagination:** Page navigation with page numbers

### ProductOverviewModel Structure
```csharp
public class ProductOverviewModel : BaseNopEntityModel
{
    public string Name/ShortDescription/SeName { get; set; }
    public string Sku { get; set; }
    public ProductType ProductType { get; set; }
    public bool MarkAsNew { get; set; }
    public ProductPriceModel ProductPrice { get; set; }
    public PictureModel DefaultPictureModel { get; set; }
    public IList<ProductSpecificationModel> SpecificationAttributeModels { get; set; }
    public ProductReviewOverviewModel ReviewOverviewModel { get; set; }
}
```

## Routing

### URL Pattern
- **Route:** `{SeName}` (from GenericUrlRouteProvider.cs)
- **Controller:** Catalog
- **Action:** Category
- **Example:** `/electronics` → Category with SeName "electronics"

### Query Parameters
- `orderby` - Sorting option ID
- `pagesize` - Items per page
- `pagenumber` - Current page
- `viewmode` - Display mode (grid/list)
- `price` - Price range filter (format: "min-max")
- `specs` - Specification filter IDs (comma-separated)

## Business Rules

### Category Visibility
1. **Published Status:** Category must be published
2. **ACL (Access Control List):** User must have access rights
3. **Store Mapping:** Category must be mapped to current store
4. **Admin Preview:** Users with "Manage categories" permission can preview unpublished

### Product Loading
1. **Visibility:** Only `visibleIndividuallyOnly: true` products
2. **Store Context:** Products must be available in current store
3. **Featured vs Regular:** 
   - Featured products loaded separately if enabled
   - Regular products exclude featured if configured
4. **Subcategory Products:** Include if `ShowProductsFromSubcategories` enabled
5. **Filtering:** Apply price range and specification filters
6. **Sorting:** Support multiple sort options
7. **Pagination:** Configurable page sizes

### Caching Strategy
- **Category Breadcrumb:** Cached by category, customer roles, store, language
- **Subcategories:** Cached by category, picture size, customer roles, store, language, SSL
- **Featured Products Existence:** Cached boolean flag for performance
- **Specification Filters:** Cached by filterable options and language

## Dependencies on Slice 1

### Existing .NET 8 Components
- ✅ Category API endpoint: `GET /api/v1/categories/{id}`
- ✅ Product API endpoint: `GET /api/v1/products/{id}`
- ✅ HttpCatalogAdapter with feature flag
- ✅ EF Core data access

### Required Extensions for Slice 2
1. **Category Products API:** `GET /api/v1/categories/{id}/products`
2. **Subcategories API:** `GET /api/v1/categories/{id}/subcategories`
3. **Featured Products API:** `GET /api/v1/categories/{id}/featured-products`
4. **Pagination Support:** Query parameters for page, size, sort
5. **Filtering Support:** Price range and specification filters

## Migration Strategy

### Phase 1: Basic Category Listing
1. Extend .NET 8 API with products endpoint
2. Add pagination and sorting support
3. Update HttpCatalogAdapter for category products

### Phase 2: Enhanced Features
1. Add subcategories support
2. Implement featured products
3. Add basic filtering (price range)

### Phase 3: Advanced Filtering
1. Specification attribute filtering
2. Complex filter combinations
3. Filter state management

## Technical Considerations

### Performance
- Heavy use of caching in original implementation
- Complex queries with multiple joins
- Specification filtering can be expensive

### Complexity Areas
1. **Specification Filtering:** Complex multi-attribute filtering logic
2. **Caching:** Multiple cache keys with various contexts
3. **Localization:** All text content is localized
4. **ACL & Store Mapping:** Complex permission checking

### Data Volume
- Categories can have hundreds of products
- Specification attributes create complex filter matrices
- Image loading for products and subcategories

## Recommended Approach

1. **Start Simple:** Basic category with products, pagination, sorting
2. **Incremental Enhancement:** Add features progressively
3. **Maintain Compatibility:** Preserve existing URLs and behavior
4. **Performance Focus:** Implement caching strategy early
5. **Feature Flag Control:** Use adapter pattern for gradual rollout