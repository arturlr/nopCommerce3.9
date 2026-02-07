using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;

namespace Nop.Services.Orders
{
    public class HttpAdminOrderAdapter
    {
        private readonly ISettingService _settingService;
        private readonly HttpClient _httpClient;
        private readonly IOrderService _fallbackService;

        public HttpAdminOrderAdapter(ISettingService settingService, IOrderService fallbackService)
        {
            _settingService = settingService;
            _fallbackService = fallbackService;
            _httpClient = new HttpClient();
        }

        public async Task<IList<Order>> GetOrdersAsync(int? orderStatus = null, DateTime? startDate = null, DateTime? endDate = null, int pageIndex = 0, int pageSize = 20)
        {
            var useDotNet8 = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
            if (!useDotNet8)
            {
                return _fallbackService.SearchOrders(0, 0, 0, orderStatus ?? 0, null, null, null, startDate, endDate, pageIndex, pageSize);
            }

            try
            {
                var url = $"http://localhost:5000/api/v1/admin/orders?pageNumber={pageIndex + 1}&pageSize={pageSize}";
                
                if (orderStatus.HasValue)
                    url += $"&orderStatus={orderStatus.Value}";
                if (startDate.HasValue)
                    url += $"&startDate={startDate.Value:yyyy-MM-ddTHH:mm:ss.fffZ}";
                if (endDate.HasValue)
                    url += $"&endDate={endDate.Value:yyyy-MM-ddTHH:mm:ss.fffZ}";

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<AdminOrderListResponse>(json);
                    
                    var orders = new List<Order>();
                    foreach (var dto in result.Orders)
                    {
                        orders.Add(new Order
                        {
                            Id = dto.Id,
                            OrderGuid = dto.OrderGuid,
                            CustomerId = dto.CustomerId,
                            OrderStatus = (OrderStatus)dto.OrderStatusId,
                            OrderTotal = dto.OrderTotal,
                            CustomerCurrencyCode = dto.CustomerCurrencyCode,
                            CreatedOnUtc = dto.CreatedOnUtc
                        });
                    }
                    return orders;
                }
                
                // Fallback on error
                return _fallbackService.SearchOrders(0, 0, 0, orderStatus ?? 0, null, null, null, startDate, endDate, pageIndex, pageSize);
            }
            catch
            {
                // Fallback on exception
                return _fallbackService.SearchOrders(0, 0, 0, orderStatus ?? 0, null, null, null, startDate, endDate, pageIndex, pageSize);
            }
        }

        public async Task<Order> UpdateOrderStatusAsync(Order order, OrderStatus newStatus)
        {
            var useDotNet8 = _settingService.GetSettingByKey<bool>("USE_DOTNET8_API", false);
            if (!useDotNet8)
            {
                order.OrderStatus = newStatus;
                _fallbackService.UpdateOrder(order);
                return order;
            }

            try
            {
                var request = new { OrderStatusId = (int)newStatus };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"http://localhost:5000/api/v1/admin/orders/{order.Id}/status", content);
                
                if (response.IsSuccessStatusCode)
                {
                    order.OrderStatus = newStatus;
                    return order;
                }
                
                // Fallback on error
                order.OrderStatus = newStatus;
                _fallbackService.UpdateOrder(order);
                return order;
            }
            catch
            {
                // Fallback on exception
                order.OrderStatus = newStatus;
                _fallbackService.UpdateOrder(order);
                return order;
            }
        }

        private class AdminOrderListResponse
        {
            public List<AdminOrderResponse> Orders { get; set; } = new();
            public int TotalCount { get; set; }
        }

        private class AdminOrderResponse
        {
            public int Id { get; set; }
            public Guid OrderGuid { get; set; }
            public int CustomerId { get; set; }
            public int OrderStatusId { get; set; }
            public string OrderStatus { get; set; } = string.Empty;
            public decimal OrderTotal { get; set; }
            public string CustomerCurrencyCode { get; set; } = "USD";
            public DateTime CreatedOnUtc { get; set; }
        }
    }
}