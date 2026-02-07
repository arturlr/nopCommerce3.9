using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities
{
    [Table("OrderItem")]
    public class OrderItem
    {
        public int Id { get; set; }
        public Guid OrderItemGuid { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceExclTax { get; set; }
        public decimal UnitPriceInclTax { get; set; }
        public decimal PriceExclTax { get; set; }
        public decimal PriceInclTax { get; set; }
        public decimal DiscountAmountExclTax { get; set; }
        public decimal DiscountAmountInclTax { get; set; }
        public string AttributeDescription { get; set; } = string.Empty;
        public string AttributesXml { get; set; } = string.Empty;

        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}