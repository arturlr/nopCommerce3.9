using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Catalog;
using Nop.Services.Configuration;

namespace Nop.Services.Catalog
{
    public class HttpAdminProductAdapter
    {
        private readonly ISettingService _settingService;
        private readonly HttpClient _httpClient;
        private readonly IProductService _fallbackService;

        public HttpAdminProductAdapter(ISettingService settingService, IProductService fallbackService)
        {
            _settingService = settingService;
            _fallbackService = fallbackService;
            _httpClient = new HttpClient();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            var useDotNet8 = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
            if (!useDotNet8)
            {
                _fallbackService.InsertProduct(product);
                return product;
            }

            try
            {
                var request = new
                {
                    Name = product.Name,
                    ShortDescription = product.ShortDescription,
                    FullDescription = product.FullDescription,
                    Sku = product.Sku,
                    Price = product.Price,
                    Published = product.Published
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("http://localhost:5000/api/v1/admin/products", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<AdminProductResponse>(responseJson);
                    
                    product.Id = result.Id;
                    return product;
                }
                
                // Fallback on error
                _fallbackService.InsertProduct(product);
                return product;
            }
            catch
            {
                // Fallback on exception
                _fallbackService.InsertProduct(product);
                return product;
            }
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            var useDotNet8 = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
            if (!useDotNet8)
            {
                _fallbackService.UpdateProduct(product);
                return product;
            }

            try
            {
                var request = new
                {
                    Name = product.Name,
                    ShortDescription = product.ShortDescription,
                    FullDescription = product.FullDescription,
                    Sku = product.Sku,
                    Price = product.Price,
                    Published = product.Published
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"http://localhost:5000/api/v1/admin/products/{product.Id}", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    // Fallback on error
                    _fallbackService.UpdateProduct(product);
                }
                
                return product;
            }
            catch
            {
                // Fallback on exception
                _fallbackService.UpdateProduct(product);
                return product;
            }
        }

        public async Task DeleteProductAsync(Product product)
        {
            var useDotNet8 = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
            if (!useDotNet8)
            {
                _fallbackService.DeleteProduct(product);
                return;
            }

            try
            {
                var response = await _httpClient.DeleteAsync($"http://localhost:5000/api/v1/admin/products/{product.Id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    // Fallback on error
                    _fallbackService.DeleteProduct(product);
                }
            }
            catch
            {
                // Fallback on exception
                _fallbackService.DeleteProduct(product);
            }
        }

        private class AdminProductResponse
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ShortDescription { get; set; }
            public string FullDescription { get; set; }
            public string Sku { get; set; }
            public decimal Price { get; set; }
            public bool Published { get; set; }
        }
    }
}