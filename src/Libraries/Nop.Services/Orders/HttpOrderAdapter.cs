using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders
{
    public class HttpOrderAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly bool _useDotNet8Api;

        public HttpOrderAdapter(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _useDotNet8Api = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            if (!_useDotNet8Api)
                return null; // Fallback to legacy

            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/orders/{orderId}");
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var orderDto = JsonConvert.DeserializeObject<OrderDto>(json);
                
                return MapToOrder(orderDto);
            }
            catch
            {
                return null; // Fallback to legacy
            }
        }

        public async Task<IList<Order>> GetCustomerOrdersAsync(int customerId, int pageIndex = 0, int pageSize = 10)
        {
            if (!_useDotNet8Api)
                return new List<Order>(); // Fallback to legacy

            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/customers/{customerId}/orders?pageNumber={pageIndex + 1}&pageSize={pageSize}");
                if (!response.IsSuccessStatusCode)
                    return new List<Order>();

                var json = await response.Content.ReadAsStringAsync();
                var customerOrdersDto = JsonConvert.DeserializeObject<CustomerOrdersDto>(json);
                
                return customerOrdersDto.Orders.Select(MapToOrder).ToList();
            }
            catch
            {
                return new List<Order>(); // Fallback to legacy
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            if (!_useDotNet8Api)
                return false; // Fallback to legacy

            try
            {
                var response = await _httpClient.PutAsync($"/api/v1/orders/{orderId}/cancel", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false; // Fallback to legacy
            }
        }

        private Order MapToOrder(OrderDto dto)
        {
            return new Order
            {
                Id = dto.Id,
                OrderGuid = dto.OrderGuid,
                CustomerId = dto.CustomerId,
                OrderStatusId = dto.OrderStatusId,
                OrderTotal = dto.OrderTotal,
                CustomerCurrencyCode = dto.CustomerCurrencyCode,
                CreatedOnUtc = dto.CreatedOnUtc
            };
        }
    }

    // DTOs for API communication
    public class OrderDto
    {
        public int Id { get; set; }
        public Guid OrderGuid { get; set; }
        public int CustomerId { get; set; }
        public int OrderStatusId { get; set; }
        public decimal OrderTotal { get; set; }
        public string CustomerCurrencyCode { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceInclTax { get; set; }
        public decimal PriceInclTax { get; set; }
    }

    public class CustomerOrdersDto
    {
        public List<OrderDto> Orders { get; set; } = new();
        public int TotalCount { get; set; }
    }
}