namespace Nop.Api8.Models;

public class OrderDto
{
    public int Id { get; set; }
    public Guid OrderGuid { get; set; }
    public int CustomerId { get; set; }
    public int OrderStatusId { get; set; }
    public decimal OrderTotal { get; set; }
    public string CustomerCurrencyCode { get; set; } = "USD";
    public DateTime CreatedOnUtc { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceInclTax { get; set; }
    public decimal PriceInclTax { get; set; }
}

public class CustomerOrdersDto
{
    public List<OrderDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
}