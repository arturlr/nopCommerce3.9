using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities
{
    [Table("ShoppingCartItem")]
    public class ShoppingCartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StoreId { get; set; }

        [Required]
        public int ShoppingCartTypeId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public string AttributesXml { get; set; } = string.Empty;

        public decimal CustomerEnteredPrice { get; set; }

        [Required]
        public int Quantity { get; set; }

        public DateTime? RentalStartDateUtc { get; set; }

        public DateTime? RentalEndDateUtc { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        [Required]
        public DateTime UpdatedOnUtc { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}