namespace Nop.Api8.Models
{
    public class PaymentProcessRequestDto
    {
        public int CustomerId { get; set; }
        public decimal OrderTotal { get; set; }
        public string PaymentMethodSystemName { get; set; }
        public string CreditCardType { get; set; }
        public string CreditCardName { get; set; }
        public string CreditCardNumber { get; set; }
        public int CreditCardExpireYear { get; set; }
        public int CreditCardExpireMonth { get; set; }
        public string CreditCardCvv2 { get; set; }
    }

    public class PaymentProcessResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string AuthorizationTransactionId { get; set; }
        public string AuthorizationTransactionCode { get; set; }
        public string AuthorizationTransactionResult { get; set; }
        public string CaptureTransactionId { get; set; }
        public string CaptureTransactionResult { get; set; }
        public decimal SubscriptionTransactionId { get; set; }
        public string PaymentStatus { get; set; }
        public bool AllowStoringCreditCardNumber { get; set; }
        public bool AllowStoringDirectDebit { get; set; }
    }
}