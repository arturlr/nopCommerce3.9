namespace Nop.Api8.Data.Entities;

public class ProductSpecificationAttribute
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int SpecificationAttributeOptionId { get; set; }
    public string CustomValue { get; set; } = string.Empty;
    public bool AllowFiltering { get; set; }
    public bool ShowOnProductPage { get; set; }
    public int DisplayOrder { get; set; }
    
    public Product Product { get; set; } = null!;
    public SpecificationAttributeOption SpecificationAttributeOption { get; set; } = null!;
}