namespace Nop.Api8.Models
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}