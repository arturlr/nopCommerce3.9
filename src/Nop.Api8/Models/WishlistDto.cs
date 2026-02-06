namespace Nop.Api8.Models
{
    public class WishlistDto
    {
        public int CustomerId { get; set; }
        public List<WishlistItemDto> Items { get; set; } = new();
    }
}