using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Orders;

namespace Nop.Services.Orders
{
    /// <summary>
    /// HTTP adapter for shopping cart operations that calls .NET 8 API with fallback to legacy service
    /// </summary>
    public class HttpShoppingCartAdapter : IShoppingCartService
    {
        private readonly IShoppingCartService _fallbackService;
        private readonly HttpClient _httpClient;
        private readonly bool _useApi;

        public HttpShoppingCartAdapter(IShoppingCartService fallbackService)
        {
            _fallbackService = fallbackService;
            _httpClient = new HttpClient();
            _useApi = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
        }

        public IList<string> AddToCart(Customer customer, Product product, ShoppingCartType shoppingCartType, 
            int storeId, string attributesXml = null, decimal customerEnteredPrice = 0, 
            DateTime? rentalStartDate = null, DateTime? rentalEndDate = null, int quantity = 1, 
            bool automaticallyAddRequiredProductsIfEnabled = true)
        {
            // Handle both ShoppingCart and Wishlist via API
            if (!_useApi || (shoppingCartType != ShoppingCartType.ShoppingCart && shoppingCartType != ShoppingCartType.Wishlist))
            {
                return _fallbackService.AddToCart(customer, product, shoppingCartType, storeId, 
                    attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate, 
                    quantity, automaticallyAddRequiredProductsIfEnabled);
            }

            try
            {
                var request = new
                {
                    customerId = customer.Id,
                    productId = product.Id,
                    quantity = quantity
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var endpoint = shoppingCartType == ShoppingCartType.Wishlist 
                    ? "http://localhost:5000/api/v1/wishlist/items"
                    : "http://localhost:5000/api/v1/cart/items";

                var response = _httpClient.PostAsync(endpoint, content).Result;
                
                if (response.IsSuccessStatusCode)
                {
                    return new List<string>(); // No warnings
                }
                else
                {
                    // Parse error response
                    var errorJson = response.Content.ReadAsStringAsync().Result;
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(errorJson);
                    
                    if (errorResponse.TryGetProperty("errors", out var errorsElement))
                    {
                        var errors = new List<string>();
                        foreach (var error in errorsElement.EnumerateArray())
                        {
                            errors.Add(error.GetString() ?? "Unknown error");
                        }
                        return errors;
                    }
                    
                    var itemType = shoppingCartType == ShoppingCartType.Wishlist ? "wishlist" : "cart";
                    return new List<string> { $"Failed to add item to {itemType}" };
                }
            }
            catch (Exception)
            {
                // Fallback to legacy service on any error
                return _fallbackService.AddToCart(customer, product, shoppingCartType, storeId, 
                    attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate, 
                    quantity, automaticallyAddRequiredProductsIfEnabled);
            }
        }

        public IList<string> UpdateShoppingCartItem(Customer customer, int shoppingCartItemId, 
            string attributesXml, decimal customerEnteredPrice, DateTime? rentalStartDate = null, 
            DateTime? rentalEndDate = null, int quantity = 1, bool resetCheckoutData = true)
        {
            if (!_useApi)
            {
                return _fallbackService.UpdateShoppingCartItem(customer, shoppingCartItemId, 
                    attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate, 
                    quantity, resetCheckoutData);
            }

            try
            {
                var request = new { quantity = quantity };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Try cart endpoint first, then wishlist endpoint
                var cartResponse = _httpClient.PutAsync($"http://localhost:5000/api/v1/cart/items/{shoppingCartItemId}", content).Result;
                
                if (cartResponse.IsSuccessStatusCode)
                {
                    return new List<string>(); // No warnings
                }

                var wishlistResponse = _httpClient.PutAsync($"http://localhost:5000/api/v1/wishlist/items/{shoppingCartItemId}", content).Result;
                
                if (wishlistResponse.IsSuccessStatusCode)
                {
                    return new List<string>(); // No warnings
                }
                
                return new List<string> { "Failed to update item" };
            }
            catch (Exception)
            {
                // Fallback to legacy service on any error
                return _fallbackService.UpdateShoppingCartItem(customer, shoppingCartItemId, 
                    attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate, 
                    quantity, resetCheckoutData);
            }
        }

        public void DeleteShoppingCartItem(ShoppingCartItem shoppingCartItem, bool resetCheckoutData = true, 
            bool ensureOnlyActiveCheckoutAttributes = false)
        {
            if (!_useApi)
            {
                _fallbackService.DeleteShoppingCartItem(shoppingCartItem, resetCheckoutData, 
                    ensureOnlyActiveCheckoutAttributes);
                return;
            }

            try
            {
                // Try cart endpoint first, then wishlist endpoint
                var cartResponse = _httpClient.DeleteAsync($"http://localhost:5000/api/v1/cart/items/{shoppingCartItem.Id}").Result;
                
                if (cartResponse.IsSuccessStatusCode)
                {
                    return;
                }

                var wishlistResponse = _httpClient.DeleteAsync($"http://localhost:5000/api/v1/wishlist/items/{shoppingCartItem.Id}").Result;
                
                if (!wishlistResponse.IsSuccessStatusCode)
                {
                    // Fallback to legacy service if both API calls fail
                    _fallbackService.DeleteShoppingCartItem(shoppingCartItem, resetCheckoutData, 
                        ensureOnlyActiveCheckoutAttributes);
                }
            }
            catch (Exception)
            {
                // Fallback to legacy service on any error
                _fallbackService.DeleteShoppingCartItem(shoppingCartItem, resetCheckoutData, 
                    ensureOnlyActiveCheckoutAttributes);
            }
        }

        // All other methods delegate to fallback service
        public int DeleteExpiredShoppingCartItems(DateTime olderThanUtc) => 
            _fallbackService.DeleteExpiredShoppingCartItems(olderThanUtc);

        public IList<string> GetRequiredProductWarnings(Customer customer, ShoppingCartType shoppingCartType, 
            Product product, int storeId, bool automaticallyAddRequiredProductsIfEnabled) => 
            _fallbackService.GetRequiredProductWarnings(customer, shoppingCartType, product, storeId, 
                automaticallyAddRequiredProductsIfEnabled);

        public IList<string> GetStandardWarnings(Customer customer, ShoppingCartType shoppingCartType, 
            Product product, string attributesXml, decimal customerEnteredPrice, int quantity) => 
            _fallbackService.GetStandardWarnings(customer, shoppingCartType, product, attributesXml, 
                customerEnteredPrice, quantity);

        public IList<string> GetShoppingCartItemAttributeWarnings(Customer customer, 
            ShoppingCartType shoppingCartType, Product product, int quantity = 1, 
            string attributesXml = "", bool ignoreNonCombinableAttributes = false) => 
            _fallbackService.GetShoppingCartItemAttributeWarnings(customer, shoppingCartType, product, 
                quantity, attributesXml, ignoreNonCombinableAttributes);

        public IList<string> GetShoppingCartItemGiftCardWarnings(ShoppingCartType shoppingCartType, 
            Product product, string attributesXml) => 
            _fallbackService.GetShoppingCartItemGiftCardWarnings(shoppingCartType, product, attributesXml);

        public IList<string> GetRentalProductWarnings(Product product, DateTime? rentalStartDate = null, 
            DateTime? rentalEndDate = null) => 
            _fallbackService.GetRentalProductWarnings(product, rentalStartDate, rentalEndDate);

        public IList<string> GetShoppingCartItemWarnings(Customer customer, ShoppingCartType shoppingCartType, 
            Product product, int storeId, string attributesXml, decimal customerEnteredPrice, 
            DateTime? rentalStartDate = null, DateTime? rentalEndDate = null, int quantity = 1, 
            bool automaticallyAddRequiredProductsIfEnabled = true, bool getStandardWarnings = true, 
            bool getAttributesWarnings = true, bool getGiftCardWarnings = true, 
            bool getRequiredProductWarnings = true, bool getRentalWarnings = true) => 
            _fallbackService.GetShoppingCartItemWarnings(customer, shoppingCartType, product, storeId, 
                attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate, quantity, 
                automaticallyAddRequiredProductsIfEnabled, getStandardWarnings, getAttributesWarnings, 
                getGiftCardWarnings, getRequiredProductWarnings, getRentalWarnings);

        public IList<string> GetShoppingCartWarnings(IList<ShoppingCartItem> shoppingCart, 
            string checkoutAttributesXml, bool validateCheckoutAttributes) => 
            _fallbackService.GetShoppingCartWarnings(shoppingCart, checkoutAttributesXml, 
                validateCheckoutAttributes);

        public ShoppingCartItem FindShoppingCartItemInTheCart(IList<ShoppingCartItem> shoppingCart, 
            ShoppingCartType shoppingCartType, Product product, string attributesXml = "", 
            decimal customerEnteredPrice = 0, DateTime? rentalStartDate = null, 
            DateTime? rentalEndDate = null) => 
            _fallbackService.FindShoppingCartItemInTheCart(shoppingCart, shoppingCartType, product, 
                attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate);

        public void MigrateShoppingCart(Customer fromCustomer, Customer toCustomer, bool includeCouponCodes) => 
            _fallbackService.MigrateShoppingCart(fromCustomer, toCustomer, includeCouponCodes);
    }
}