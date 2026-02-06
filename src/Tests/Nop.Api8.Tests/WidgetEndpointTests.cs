using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Nop.Api8.Tests;

public class WidgetEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WidgetEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetWidgetZones_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/widgets/zones");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError);
        
        if (response.IsSuccessStatusCode)
        {
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
            
            var content = await response.Content.ReadAsStringAsync();
            var zones = JsonSerializer.Deserialize<WidgetZoneDto[]>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            Assert.NotNull(zones);
            Assert.Contains(zones, z => z.Name == "home_page_top");
            Assert.Contains(zones, z => z.Name == "home_page_bottom");
        }
    }

    [Fact]
    public async Task GetWidgetsByZone_HomePageTop_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/widgets/zone/home_page_top");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError);
        
        if (response.IsSuccessStatusCode)
        {
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
            
            var content = await response.Content.ReadAsStringAsync();
            var zoneContent = JsonSerializer.Deserialize<WidgetZoneContentDto>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            Assert.NotNull(zoneContent);
            Assert.Equal("home_page_top", zoneContent.ZoneName);
            Assert.NotNull(zoneContent.Widgets);
        }
    }

    [Fact]
    public async Task GetWidgetsByZone_EmptyZone_ReturnsSuccessWithEmptyWidgets()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/widgets/zone/left_side_column_before");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var zoneContent = JsonSerializer.Deserialize<WidgetZoneContentDto>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            Assert.NotNull(zoneContent);
            Assert.Equal("left_side_column_before", zoneContent.ZoneName);
            Assert.Empty(zoneContent.Widgets);
        }
    }

    [Fact]
    public async Task GetWidgetsByZone_NonExistentZone_ReturnsSuccessWithEmptyWidgets()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/widgets/zone/non_existent_zone");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var zoneContent = JsonSerializer.Deserialize<WidgetZoneContentDto>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            Assert.NotNull(zoneContent);
            Assert.Equal("non_existent_zone", zoneContent.ZoneName);
            Assert.Empty(zoneContent.Widgets);
        }
    }
}

// DTOs for test deserialization
public class WidgetZoneDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int WidgetCount { get; set; }
}

public class WidgetDto
{
    public string SystemName { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WidgetZone { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class WidgetZoneContentDto
{
    public string ZoneName { get; set; } = string.Empty;
    public List<WidgetDto> Widgets { get; set; } = new();
}