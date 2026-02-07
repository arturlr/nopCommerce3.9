namespace Nop.Api8.Models;

public class ProductReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReviewText { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}