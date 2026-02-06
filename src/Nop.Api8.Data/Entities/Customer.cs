using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities
{
    [Table("Customer")]
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Email { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Password { get; set; } = string.Empty;

        public int PasswordFormatId { get; set; }

        public Guid CustomerGuid { get; set; }

        public bool Active { get; set; }

        public bool Deleted { get; set; }

        public bool IsSystemAccount { get; set; }

        [StringLength(400)]
        public string SystemName { get; set; } = string.Empty;

        public DateTime? LastIpAddress { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime LastLoginDateUtc { get; set; }

        public DateTime LastActivityDateUtc { get; set; }

        public int RegisteredInStoreId { get; set; }

        // Navigation properties
        public virtual ICollection<GenericAttribute> GenericAttributes { get; set; } = new List<GenericAttribute>();
    }
}