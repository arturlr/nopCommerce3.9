using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities;

[Table("Product_Category_Mapping")]
public class ProductCategory
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public bool IsFeaturedProduct { get; set; }
    public int DisplayOrder { get; set; }
    
    public Product Product { get; set; } = null!;
    public Category Category { get; set; } = null!;
}