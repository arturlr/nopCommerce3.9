using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities;

[Table("Product")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool Published { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    
    public List<ProductCategory> ProductCategories { get; set; } = new();
    public List<ProductPicture> ProductPictures { get; set; } = new();
    public List<ProductSpecificationAttribute> ProductSpecificationAttributes { get; set; } = new();
    public List<ProductReview> ProductReviews { get; set; } = new();
}