namespace Nop.Api8.Models
{
    public class CheckoutValidationDto
    {
        public int CustomerId { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }
}