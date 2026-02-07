namespace Nop.Api8.Models;

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