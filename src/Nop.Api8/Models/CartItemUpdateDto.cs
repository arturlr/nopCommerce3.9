using System.ComponentModel.DataAnnotations;

namespace Nop.Api8.Models
{
    public class CartItemUpdateDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
    }
}