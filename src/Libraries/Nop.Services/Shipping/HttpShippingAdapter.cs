using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Configuration;
using Nop.Services.Shipping;

namespace Nop.Services.Shipping
{
    /// <summary>
    /// HTTP adapter for shipping service integration with .NET 8 API
    /// </summary>
    public class HttpShippingAdapter : IShippingService
    {
        private readonly IShippingService _fallbackService;
        private readonly ISettingService _settingService;
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "http://localhost:5000/api/v1";

        public HttpShippingAdapter(IShippingService fallbackService, ISettingService settingService)
        {
            _fallbackService = fallbackService;
            _settingService = settingService;
            _httpClient = new HttpClient();
        }

        #region Shipping rate computation methods

        public IList<IShippingRateComputationMethod> LoadActiveShippingRateComputationMethods(Customer customer = null, int storeId = 0)
        {
            return _fallbackService.LoadActiveShippingRateComputationMethods(customer, storeId);
        }

        public IShippingRateComputationMethod LoadShippingRateComputationMethodBySystemName(string systemName)
        {
            return _fallbackService.LoadShippingRateComputationMethodBySystemName(systemName);
        }

        public IList<IShippingRateComputationMethod> LoadAllShippingRateComputationMethods(Customer customer = null, int storeId = 0)
        {
            return _fallbackService.LoadAllShippingRateComputationMethods(customer, storeId);
        }

        #endregion

        #region Shipping methods

        public void DeleteShippingMethod(ShippingMethod shippingMethod)
        {
            _fallbackService.DeleteShippingMethod(shippingMethod);
        }

        public ShippingMethod GetShippingMethodById(int shippingMethodId)
        {
            return _fallbackService.GetShippingMethodById(shippingMethodId);
        }

        /// <summary>
        /// Gets all shipping methods - enhanced with .NET 8 API integration
        /// </summary>
        public IList<ShippingMethod> GetAllShippingMethods(int? filterByCountryId = null)
        {
            try
            {
                var useApi = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
                if (useApi)
                {
                    var url = $"{API_BASE_URL}/shipping/methods";
                    if (filterByCountryId.HasValue)
                        url += $"?countryId={filterByCountryId.Value}";

                    var response = _httpClient.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        var apiMethods = JsonConvert.DeserializeObject<List<ShippingMethodDto>>(json);
                        
                        return apiMethods.Select(dto => new ShippingMethod
                        {
                            Id = dto.Id,
                            Name = dto.Name,
                            Description = dto.Description,
                            DisplayOrder = dto.DisplayOrder
                        }).ToList();
                    }
                }
            }
            catch (Exception)
            {
                // Fall back to legacy service on any error
            }

            return _fallbackService.GetAllShippingMethods(filterByCountryId);
        }

        public void InsertShippingMethod(ShippingMethod shippingMethod)
        {
            _fallbackService.InsertShippingMethod(shippingMethod);
        }

        public void UpdateShippingMethod(ShippingMethod shippingMethod)
        {
            _fallbackService.UpdateShippingMethod(shippingMethod);
        }

        #endregion

        #region Warehouses

        public void DeleteWarehouse(Warehouse warehouse)
        {
            _fallbackService.DeleteWarehouse(warehouse);
        }

        public Warehouse GetWarehouseById(int warehouseId)
        {
            return _fallbackService.GetWarehouseById(warehouseId);
        }

        public IList<Warehouse> GetAllWarehouses()
        {
            return _fallbackService.GetAllWarehouses();
        }

        public void InsertWarehouse(Warehouse warehouse)
        {
            _fallbackService.InsertWarehouse(warehouse);
        }

        public void UpdateWarehouse(Warehouse warehouse)
        {
            _fallbackService.UpdateWarehouse(warehouse);
        }

        #endregion

        #region Pickup points

        public IList<Pickup.IPickupPointProvider> LoadActivePickupPointProviders(Customer customer = null, int storeId = 0)
        {
            return _fallbackService.LoadActivePickupPointProviders(customer, storeId);
        }

        public Pickup.IPickupPointProvider LoadPickupPointProviderBySystemName(string systemName)
        {
            return _fallbackService.LoadPickupPointProviderBySystemName(systemName);
        }

        public IList<Pickup.IPickupPointProvider> LoadAllPickupPointProviders(Customer customer = null, int storeId = 0)
        {
            return _fallbackService.LoadAllPickupPointProviders(customer, storeId);
        }

        #endregion

        #region Workflow

        public decimal GetShoppingCartItemWeight(ShoppingCartItem shoppingCartItem)
        {
            return _fallbackService.GetShoppingCartItemWeight(shoppingCartItem);
        }

        public decimal GetTotalWeight(GetShippingOptionRequest request, bool includeCheckoutAttributes = true)
        {
            return _fallbackService.GetTotalWeight(request, includeCheckoutAttributes);
        }

        public void GetAssociatedProductDimensions(ShoppingCartItem shoppingCartItem, out decimal width, out decimal length, out decimal height)
        {
            _fallbackService.GetAssociatedProductDimensions(shoppingCartItem, out width, out length, out height);
        }

        public void GetDimensions(IList<GetShippingOptionRequest.PackageItem> packageItems, out decimal width, out decimal length, out decimal height)
        {
            _fallbackService.GetDimensions(packageItems, out width, out length, out height);
        }

        public Warehouse GetNearestWarehouse(Address address, IList<Warehouse> warehouses = null)
        {
            return _fallbackService.GetNearestWarehouse(address, warehouses);
        }

        public IList<GetShippingOptionRequest> CreateShippingOptionRequests(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId, out bool shippingFromMultipleLocations)
        {
            return _fallbackService.CreateShippingOptionRequests(cart, shippingAddress, storeId, out shippingFromMultipleLocations);
        }

        /// <summary>
        /// Gets available shipping options - enhanced with .NET 8 API integration
        /// </summary>
        public GetShippingOptionResponse GetShippingOptions(IList<ShoppingCartItem> cart, Address shippingAddress, Customer customer = null, string allowedShippingRateComputationMethodSystemName = "", int storeId = 0)
        {
            try
            {
                var useApi = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
                if (useApi && cart.Any())
                {
                    var request = new ShippingRateRequestDto
                    {
                        CustomerId = customer?.Id ?? 0,
                        StoreId = storeId,
                        Items = cart.Select(ci => new ShippingCartItemDto
                        {
                            ProductId = ci.ProductId,
                            Quantity = ci.Quantity
                        }).ToList(),
                        ShippingAddress = new ShippingAddressDto
                        {
                            Address1 = shippingAddress?.Address1 ?? "",
                            City = shippingAddress?.City ?? "",
                            StateProvince = shippingAddress?.StateProvince?.Name ?? "",
                            ZipPostalCode = shippingAddress?.ZipPostalCode ?? "",
                            CountryId = shippingAddress?.CountryId ?? 0
                        }
                    };

                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = _httpClient.PostAsync($"{API_BASE_URL}/shipping/calculate", content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = response.Content.ReadAsStringAsync().Result;
                        var apiResult = JsonConvert.DeserializeObject<ShippingRateResultDto>(responseJson);
                        
                        if (apiResult.Success)
                        {
                            var shippingResponse = new GetShippingOptionResponse();
                            foreach (var option in apiResult.ShippingOptions)
                            {
                                shippingResponse.ShippingOptions.Add(new ShippingOption
                                {
                                    Name = option.Name,
                                    Description = option.Description,
                                    Rate = option.Rate,
                                    ShippingRateComputationMethodSystemName = option.ShippingRateComputationMethodSystemName
                                });
                            }
                            return shippingResponse;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fall back to legacy service on any error
            }

            return _fallbackService.GetShippingOptions(cart, shippingAddress, customer, allowedShippingRateComputationMethodSystemName, storeId);
        }

        public Pickup.GetPickupPointsResponse GetPickupPoints(Address address, Customer customer = null, string providerSystemName = null, int storeId = 0)
        {
            return _fallbackService.GetPickupPoints(address, customer, providerSystemName, storeId);
        }

        #endregion

        #region DTOs for API communication

        private class ShippingMethodDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int DisplayOrder { get; set; }
        }

        private class ShippingRateRequestDto
        {
            public int CustomerId { get; set; }
            public List<ShippingCartItemDto> Items { get; set; } = new List<ShippingCartItemDto>();
            public ShippingAddressDto ShippingAddress { get; set; }
            public int StoreId { get; set; } = 1;
        }

        private class ShippingCartItemDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        private class ShippingAddressDto
        {
            public string Address1 { get; set; }
            public string City { get; set; }
            public string StateProvince { get; set; }
            public string ZipPostalCode { get; set; }
            public int CountryId { get; set; }
        }

        private class ShippingRateResultDto
        {
            public List<ShippingOptionDto> ShippingOptions { get; set; } = new List<ShippingOptionDto>();
            public bool Success { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }

        private class ShippingOptionDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Rate { get; set; }
            public string ShippingRateComputationMethodSystemName { get; set; }
        }

        #endregion
    }
}