using System.ComponentModel.DataAnnotations;

namespace Nop.Api8.Models
{
    public class WishlistItemRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be greater than 0")]
        public int CustomerId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId must be greater than 0")]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
    }
}