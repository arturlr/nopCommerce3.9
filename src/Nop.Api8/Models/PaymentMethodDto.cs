namespace Nop.Api8.Models
{
    public class PaymentMethodDto
    {
        public string SystemName { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public bool SkipPaymentInfo { get; set; }
        public string PaymentMethodType { get; set; }
    }
}