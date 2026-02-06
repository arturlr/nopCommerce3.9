using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Logging;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// HTTP adapter for catalog services that calls .NET 8 API with fallback to original service
    /// </summary>
    public partial class HttpCatalogAdapter : ICategoryService, IDisposable
    {
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly bool _useDotNet8Api;

        public HttpCatalogAdapter(ICategoryService categoryService, IProductService productService, ILogger logger)
        {
            _categoryService = categoryService;
            _productService = productService;
            _logger = logger;
            _httpClient = new HttpClient 
            { 
                BaseAddress = new Uri("http://localhost:5000"),
                Timeout = TimeSpan.FromSeconds(5)
            };
            _useDotNet8Api = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
        }

        public virtual Category GetCategoryById(int categoryId)
        {
            if (!_useDotNet8Api)
                return _categoryService.GetCategoryById(categoryId);

            try
            {
                var response = _httpClient.GetAsync($"/api/v1/categories/{categoryId}").Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    var dto = JsonSerializer.Deserialize<CategoryDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new Category
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        Description = dto.Description,
                        SeName = dto.SeName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Warning, "HttpCatalogAdapter.GetCategoryById failed", ex.Message);
            }

            return _categoryService.GetCategoryById(categoryId);
        }

        // Write operations - always delegate to original service
        public virtual void DeleteCategory(Category category) => _categoryService.DeleteCategory(category);
        public virtual void InsertCategory(Category category) => _categoryService.InsertCategory(category);
        public virtual void UpdateCategory(Category category) => _categoryService.UpdateCategory(category);

        // Read operations - delegate to original service (no .NET 8 API endpoints yet)
        public virtual IPagedList<Category> GetAllCategories(string categoryName = "", int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
            => _categoryService.GetAllCategories(categoryName, storeId, pageIndex, pageSize, showHidden);

        public virtual IList<Category> GetAllCategoriesByParentCategoryId(int parentCategoryId, bool showHidden = false, bool includeAllLevels = false)
            => _categoryService.GetAllCategoriesByParentCategoryId(parentCategoryId, showHidden, includeAllLevels);

        public virtual IList<Category> GetAllCategoriesDisplayedOnHomePage(bool showHidden = false)
            => _categoryService.GetAllCategoriesDisplayedOnHomePage(showHidden);

        // ProductCategory operations - delegate to original service
        public virtual void DeleteProductCategory(ProductCategory productCategory) => _categoryService.DeleteProductCategory(productCategory);
        public virtual void InsertProductCategory(ProductCategory productCategory) => _categoryService.InsertProductCategory(productCategory);
        public virtual void UpdateProductCategory(ProductCategory productCategory) => _categoryService.UpdateProductCategory(productCategory);

        public virtual IPagedList<ProductCategory> GetProductCategoriesByCategoryId(int categoryId, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
            => _categoryService.GetProductCategoriesByCategoryId(categoryId, pageIndex, pageSize, showHidden);

        public virtual IList<ProductCategory> GetProductCategoriesByProductId(int productId, bool showHidden = false)
            => _categoryService.GetProductCategoriesByProductId(productId, showHidden);

        public virtual IList<ProductCategory> GetProductCategoriesByProductId(int productId, int storeId, bool showHidden = false)
            => _categoryService.GetProductCategoriesByProductId(productId, storeId, showHidden);

        public virtual ProductCategory GetProductCategoryById(int productCategoryId)
            => _categoryService.GetProductCategoryById(productCategoryId);

        public virtual string[] GetNotExistingCategories(string[] categoryNames)
            => _categoryService.GetNotExistingCategories(categoryNames);

        public virtual IDictionary<int, int[]> GetProductCategoryIds(int[] productIds)
            => _categoryService.GetProductCategoryIds(productIds);

        public virtual Product GetProductById(int productId)
        {
            if (!_useDotNet8Api)
                return _productService.GetProductById(productId);

            try
            {
                var response = _httpClient.GetAsync($"/api/v1/products/{productId}").Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    var dto = JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new Product
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        ShortDescription = dto.ShortDescription,
                        Sku = dto.Sku,
                        Price = dto.Price
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Warning, "HttpCatalogAdapter.GetProductById failed", ex.Message);
            }

            return _productService.GetProductById(productId);
        }

        /// <summary>
        /// Gets detailed product information including images, specifications, and reviews summary
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <returns>Product with detailed information or null if not found</returns>
        public virtual ProductDetailsDto GetProductDetails(int productId)
        {
            if (!_useDotNet8Api)
                return null;

            try
            {
                var response = _httpClient.GetAsync($"/api/v1/products/{productId}/details").Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    return JsonSerializer.Deserialize<ProductDetailsDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Warning, "HttpCatalogAdapter.GetProductDetails failed", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Gets products for a category with pagination
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="priceMin">Minimum price</param>
        /// <param name="priceMax">Maximum price</param>
        /// <param name="featuredOnly">Featured products only</param>
        /// <returns>Paginated products</returns>
        public virtual IPagedList<Product> GetCategoryProducts(int categoryId, int pageIndex = 0, int pageSize = 6, 
            string orderBy = "position", decimal? priceMin = null, decimal? priceMax = null, bool? featuredOnly = null)
        {
            if (!_useDotNet8Api)
                return _productService.SearchProducts(pageIndex: pageIndex, pageSize: pageSize, categoryIds: new List<int> { categoryId }, 
                    priceMin: priceMin, priceMax: priceMax, featuredProducts: featuredOnly);

            try
            {
                var query = $"/api/v1/categories/{categoryId}/products?pageNumber={pageIndex + 1}&pageSize={pageSize}&orderBy={orderBy}";
                if (priceMin.HasValue) query += $"&priceMin={priceMin}";
                if (priceMax.HasValue) query += $"&priceMax={priceMax}";
                if (featuredOnly.HasValue) query += $"&featuredOnly={featuredOnly}";

                var response = _httpClient.GetAsync(query).Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    var dto = JsonSerializer.Deserialize<CategoryProductsDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var products = dto.Products.Select(p => new Product
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ShortDescription = p.ShortDescription,
                        Sku = p.Sku,
                        Price = p.Price
                    }).ToList();

                    return new PagedList<Product>(products, pageIndex, pageSize, dto.Pagination.TotalItems);
                }
            }
            catch (Exception ex)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Warning, "HttpCatalogAdapter.GetCategoryProducts failed", ex.Message);
            }

            return _productService.SearchProducts(pageIndex: pageIndex, pageSize: pageSize, categoryIds: new List<int> { categoryId }, 
                priceMin: priceMin, priceMax: priceMax, featuredProducts: featuredOnly);
        }

        /// <summary>
        /// Search products with text query and filters
        /// </summary>
        /// <param name="keywords">Search keywords</param>
        /// <param name="categoryId">Category filter</param>
        /// <param name="priceMin">Minimum price</param>
        /// <param name="priceMax">Maximum price</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated search results</returns>
        public virtual IPagedList<Product> SearchProducts(string keywords = null, int? categoryId = null, 
            decimal? priceMin = null, decimal? priceMax = null, int pageIndex = 0, int pageSize = 10)
        {
            if (!_useDotNet8Api)
                return _productService.SearchProducts(pageIndex: pageIndex, pageSize: pageSize, 
                    keywords: keywords, categoryIds: categoryId.HasValue ? new List<int> { categoryId.Value } : null,
                    priceMin: priceMin, priceMax: priceMax, searchDescriptions: true);

            try
            {
                var query = $"/api/v1/products/search?pageNumber={pageIndex + 1}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(keywords)) query += $"&q={Uri.EscapeDataString(keywords)}";
                if (categoryId.HasValue) query += $"&categoryId={categoryId}";
                if (priceMin.HasValue) query += $"&minPrice={priceMin}";
                if (priceMax.HasValue) query += $"&maxPrice={priceMax}";

                var response = _httpClient.GetAsync(query).Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    var dto = JsonSerializer.Deserialize<ProductSearchResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var products = dto.Products.Select(p => new Product
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ShortDescription = p.ShortDescription,
                        Sku = p.Sku,
                        Price = p.Price
                    }).ToList();

                    return new PagedList<Product>(products, pageIndex, pageSize, dto.Pagination.TotalItems);
                }
            }
            catch (Exception ex)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Warning, "HttpCatalogAdapter.SearchProducts failed", ex.Message);
            }

            return _productService.SearchProducts(pageIndex: pageIndex, pageSize: pageSize, 
                keywords: keywords, categoryIds: categoryId.HasValue ? new List<int> { categoryId.Value } : null,
                priceMin: priceMin, priceMax: priceMax, searchDescriptions: true);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        private class CategoryDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string SeName { get; set; } = string.Empty;
        }

        private class ProductDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string ShortDescription { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

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

        private class CategoryProductsDto
        {
            public ProductDto[] Products { get; set; } = Array.Empty<ProductDto>();
            public PaginationMetadata Pagination { get; set; } = new();
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
        }

        private class PaginationMetadata
        {
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
        }

        private class ProductSearchResponseDto
        {
            public ProductDto[] Products { get; set; } = Array.Empty<ProductDto>();
            public PaginationResponseDto Pagination { get; set; } = new();
            public string SearchQuery { get; set; } = string.Empty;
        }

        private class PaginationResponseDto
        {
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
        }
    }
}