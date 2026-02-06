using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities
{
    [Table("ShippingMethod")]
    public class ShippingMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(400)]
        public string Name { get; set; }

        [StringLength(4000)]
        public string Description { get; set; }

        public int DisplayOrder { get; set; }
    }
}