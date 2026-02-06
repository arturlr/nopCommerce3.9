using System.ComponentModel.DataAnnotations;

namespace Nop.Api8.Models
{
    public class CustomerUpdateDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; }
    }
}