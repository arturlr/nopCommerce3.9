namespace Nop.Api8.Data.Entities;

public class SpecificationAttributeOption
{
    public int Id { get; set; }
    public int SpecificationAttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    
    public SpecificationAttribute SpecificationAttribute { get; set; } = null!;
}