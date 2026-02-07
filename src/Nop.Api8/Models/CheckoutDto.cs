namespace Nop.Api8.Models
{
    public class CheckoutCompleteRequestDto
    {
        public int CustomerId { get; set; }
        public int BillingAddressId { get; set; }
        public int? ShippingAddressId { get; set; }
    }

    public class CheckoutCompleteResponseDto
    {
        public int OrderId { get; set; }
        public Guid OrderGuid { get; set; }
        public decimal OrderTotal { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedOnUtc { get; set; }
    }
}