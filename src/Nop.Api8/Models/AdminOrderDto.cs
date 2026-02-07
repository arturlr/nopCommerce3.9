namespace Nop.Api8.Models;

public class AdminOrderDto
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

public class AdminOrderListDto
{
    public List<AdminOrderDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
}

public class UpdateOrderStatusRequest
{
    public int OrderStatusId { get; set; }
}