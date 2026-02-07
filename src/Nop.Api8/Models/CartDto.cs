namespace Nop.Api8.Models
{
    public class CartDto
    {
        public int CustomerId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }
}