using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities
{
    [Table("GenericAttribute")]
    public class GenericAttribute
    {
        [Key]
        public int Id { get; set; }

        public int EntityId { get; set; }

        [Required]
        [StringLength(400)]
        public string KeyGroup { get; set; } = string.Empty;

        [Required]
        [StringLength(400)]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public int StoreId { get; set; }

        // Navigation properties
        public virtual Customer? Customer { get; set; }
    }
}