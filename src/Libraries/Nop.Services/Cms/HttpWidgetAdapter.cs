using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Customers;
using Nop.Services.Cms;

namespace Nop.Services.Cms
{
    /// <summary>
    /// HTTP adapter for widget service that delegates to .NET 8 API with fallback to legacy service
    /// </summary>
    public class HttpWidgetAdapter : IWidgetService
    {
        private readonly IWidgetService _legacyService;
        private readonly HttpClient _httpClient;
        private readonly bool _useDotNet8Api;

        public HttpWidgetAdapter(IWidgetService legacyService)
        {
            _legacyService = legacyService;
            _httpClient = new HttpClient();
            _useDotNet8Api = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
        }

        public IList<IWidgetPlugin> LoadActiveWidgets(Customer customer = null, int storeId = 0)
        {
            // Always delegate to legacy service for plugin loading
            return _legacyService.LoadActiveWidgets(customer, storeId);
        }

        public IList<IWidgetPlugin> LoadActiveWidgetsByWidgetZone(string widgetZone, Customer customer = null, int storeId = 0)
        {
            if (_useDotNet8Api)
            {
                try
                {
                    // Try .NET 8 API first
                    var task = GetWidgetsByZoneAsync(widgetZone);
                    task.Wait(TimeSpan.FromSeconds(5));
                    
                    if (task.IsCompletedSuccessfully)
                    {
                        // Convert API response to legacy format if needed
                        // For now, fall back to legacy service for actual widget instances
                        return _legacyService.LoadActiveWidgetsByWidgetZone(widgetZone, customer, storeId);
                    }
                }
                catch (Exception)
                {
                    // Fall back to legacy service on error
                }
            }

            return _legacyService.LoadActiveWidgetsByWidgetZone(widgetZone, customer, storeId);
        }

        public IWidgetPlugin LoadWidgetBySystemName(string systemName)
        {
            return _legacyService.LoadWidgetBySystemName(systemName);
        }

        public IList<IWidgetPlugin> LoadAllWidgets(Customer customer = null, int storeId = 0)
        {
            return _legacyService.LoadAllWidgets(customer, storeId);
        }

        private async Task<WidgetZoneContentDto> GetWidgetsByZoneAsync(string zoneName)
        {
            var response = await _httpClient.GetAsync($"http://localhost:5000/api/v1/widgets/zone/{zoneName}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<WidgetZoneContentDto>(json);
        }

        private async Task<List<WidgetZoneDto>> GetWidgetZonesAsync()
        {
            var response = await _httpClient.GetAsync("http://localhost:5000/api/v1/widgets/zones");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<WidgetZoneDto>>(json);
        }
    }

    // DTOs for API communication
    public class WidgetZoneDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int WidgetCount { get; set; }
    }

    public class WidgetDto
    {
        public string SystemName { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string WidgetZone { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public bool IsActive { get; set; }
    }

    public class WidgetZoneContentDto
    {
        public string ZoneName { get; set; }
        public List<WidgetDto> Widgets { get; set; } = new List<WidgetDto>();
    }
}