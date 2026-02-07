namespace Nop.Api8.Models;

public class ProductReviewRequestDto
{
    public int CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReviewText { get; set; } = string.Empty;
    public int Rating { get; set; }
}