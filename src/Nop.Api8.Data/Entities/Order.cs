using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities
{
    [Table("Order")]
    public class Order
    {
        public int Id { get; set; }
        public Guid OrderGuid { get; set; }
        public int StoreId { get; set; }
        public int CustomerId { get; set; }
        public int BillingAddressId { get; set; }
        public int? ShippingAddressId { get; set; }
        public int OrderStatusId { get; set; }
        public int PaymentStatusId { get; set; }
        public int ShippingStatusId { get; set; }
        public string CustomerCurrencyCode { get; set; } = "USD";
        public decimal CurrencyRate { get; set; } = 1.0m;
        public decimal OrderSubtotalExclTax { get; set; }
        public decimal OrderSubtotalInclTax { get; set; }
        public decimal OrderShippingExclTax { get; set; }
        public decimal OrderShippingInclTax { get; set; }
        public decimal OrderTax { get; set; }
        public decimal OrderTotal { get; set; }
        public string PaymentMethodSystemName { get; set; } = string.Empty;
        public DateTime CreatedOnUtc { get; set; }
        public bool Deleted { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}