namespace Nop.Api8.Data.Entities;

public class ProductPicture
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int PictureId { get; set; }
    public int DisplayOrder { get; set; }
    
    public Product Product { get; set; } = null!;
    public Picture Picture { get; set; } = null!;
}