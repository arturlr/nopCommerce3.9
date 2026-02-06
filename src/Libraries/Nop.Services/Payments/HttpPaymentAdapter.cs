using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Payments;

namespace Nop.Services.Payments
{
    /// <summary>
    /// HTTP adapter for payment operations with .NET 8 API fallback
    /// </summary>
    public class HttpPaymentAdapter : IPaymentService
    {
        private readonly IPaymentService _fallbackService;
        private readonly HttpClient _httpClient;
        private readonly bool _useNet8Api;

        public HttpPaymentAdapter(IPaymentService fallbackService, HttpClient httpClient)
        {
            _fallbackService = fallbackService;
            _httpClient = httpClient;
            _useNet8Api = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
        }

        public IList<IPaymentMethod> LoadActivePaymentMethods(Customer customer = null, int storeId = 0, int filterByCountryId = 0)
        {
            if (_useNet8Api)
            {
                try
                {
                    var url = $"http://localhost:5000/api/v1/payments/methods";
                    if (customer != null)
                        url += $"?customerId={customer.Id}";

                    var response = _httpClient.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        var methods = JsonConvert.DeserializeObject<List<PaymentMethodDto>>(json);
                        
                        // Convert DTOs to mock payment methods for compatibility
                        return methods.Select(m => new MockPaymentMethod
                        {
                            SystemName = m.SystemName,
                            FriendlyName = m.FriendlyName,
                            Description = m.Description
                        }).Cast<IPaymentMethod>().ToList();
                    }
                }
                catch
                {
                    // Fall back to legacy service on error
                }
            }

            return _fallbackService.LoadActivePaymentMethods(customer, storeId, filterByCountryId);
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            if (_useNet8Api)
            {
                try
                {
                    var requestDto = new PaymentProcessRequestDto
                    {
                        CustomerId = processPaymentRequest.CustomerId,
                        OrderTotal = processPaymentRequest.OrderTotal,
                        PaymentMethodSystemName = processPaymentRequest.PaymentMethodSystemName,
                        CreditCardType = processPaymentRequest.CreditCardType,
                        CreditCardName = processPaymentRequest.CreditCardName,
                        CreditCardNumber = processPaymentRequest.CreditCardNumber,
                        CreditCardExpireYear = processPaymentRequest.CreditCardExpireYear,
                        CreditCardExpireMonth = processPaymentRequest.CreditCardExpireMonth,
                        CreditCardCvv2 = processPaymentRequest.CreditCardCvv2
                    };

                    var json = JsonConvert.SerializeObject(requestDto);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = _httpClient.PostAsync("http://localhost:5000/api/v1/payments/process", content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = response.Content.ReadAsStringAsync().Result;
                        var resultDto = JsonConvert.DeserializeObject<PaymentProcessResultDto>(responseJson);
                        
                        return new ProcessPaymentResult
                        {
                            NewPaymentStatus = resultDto.Success ? 
                                (resultDto.PaymentStatus == "Authorized" ? Core.Domain.Payments.PaymentStatus.Authorized : Core.Domain.Payments.PaymentStatus.Pending) :
                                Core.Domain.Payments.PaymentStatus.Pending,
                            AuthorizationTransactionId = resultDto.AuthorizationTransactionId,
                            AuthorizationTransactionCode = resultDto.AuthorizationTransactionCode,
                            AuthorizationTransactionResult = resultDto.AuthorizationTransactionResult,
                            CaptureTransactionId = resultDto.CaptureTransactionId,
                            CaptureTransactionResult = resultDto.CaptureTransactionResult,
                            SubscriptionTransactionId = resultDto.SubscriptionTransactionId,
                            AllowStoringCreditCardNumber = resultDto.AllowStoringCreditCardNumber,
                            AllowStoringDirectDebit = resultDto.AllowStoringDirectDebit,
                            Errors = resultDto.Success ? new List<string>() : new List<string> { resultDto.ErrorMessage ?? "Payment failed" }
                        };
                    }
                }
                catch
                {
                    // Fall back to legacy service on error
                }
            }

            return _fallbackService.ProcessPayment(processPaymentRequest);
        }

        // Delegate all other methods to fallback service
        public IPaymentMethod LoadPaymentMethodBySystemName(string systemName) => _fallbackService.LoadPaymentMethodBySystemName(systemName);
        public IList<IPaymentMethod> LoadAllPaymentMethods(Customer customer = null, int storeId = 0, int filterByCountryId = 0) => _fallbackService.LoadAllPaymentMethods(customer, storeId, filterByCountryId);
        public IList<int> GetRestictedCountryIds(IPaymentMethod paymentMethod) => _fallbackService.GetRestictedCountryIds(paymentMethod);
        public void SaveRestictedCountryIds(IPaymentMethod paymentMethod, List<int> countryIds) => _fallbackService.SaveRestictedCountryIds(paymentMethod, countryIds);
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest) => _fallbackService.PostProcessPayment(postProcessPaymentRequest);
        public bool CanRePostProcessPayment(Order order) => _fallbackService.CanRePostProcessPayment(order);
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart, string paymentMethodSystemName) => _fallbackService.GetAdditionalHandlingFee(cart, paymentMethodSystemName);
        public bool SupportCapture(string paymentMethodSystemName) => _fallbackService.SupportCapture(paymentMethodSystemName);
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest) => _fallbackService.Capture(capturePaymentRequest);
        public bool SupportPartiallyRefund(string paymentMethodSystemName) => _fallbackService.SupportPartiallyRefund(paymentMethodSystemName);
        public bool SupportRefund(string paymentMethodSystemName) => _fallbackService.SupportRefund(paymentMethodSystemName);
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest) => _fallbackService.Refund(refundPaymentRequest);
        public bool SupportVoid(string paymentMethodSystemName) => _fallbackService.SupportVoid(paymentMethodSystemName);
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest) => _fallbackService.Void(voidPaymentRequest);
        public RecurringPaymentType GetRecurringPaymentType(string paymentMethodSystemName) => _fallbackService.GetRecurringPaymentType(paymentMethodSystemName);
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest) => _fallbackService.ProcessRecurringPayment(processPaymentRequest);
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest) => _fallbackService.CancelRecurringPayment(cancelPaymentRequest);
        public string GetMaskedCreditCardNumber(string creditCardNumber) => _fallbackService.GetMaskedCreditCardNumber(creditCardNumber);
    }

    // DTOs for API communication
    public class PaymentMethodDto
    {
        public string SystemName { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public bool SkipPaymentInfo { get; set; }
        public string PaymentMethodType { get; set; }
    }

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

    // Mock payment method for compatibility
    public class MockPaymentMethod : IPaymentMethod
    {
        public string SystemName { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest) => throw new NotImplementedException();
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest) => throw new NotImplementedException();
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart) => false;
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart) => 0;
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest) => throw new NotImplementedException();
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest) => throw new NotImplementedException();
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest) => throw new NotImplementedException();
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest) => throw new NotImplementedException();
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest) => throw new NotImplementedException();
        public bool CanRePostProcessPayment(Order order) => false;
        public void GetConfigurationRoute(out string actionName, out string controllerName, out System.Web.Routing.RouteValueDictionary routeValues)
        {
            actionName = null;
            controllerName = null;
            routeValues = null;
        }
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out System.Web.Routing.RouteValueDictionary routeValues)
        {
            actionName = null;
            controllerName = null;
            routeValues = null;
        }
        public Type GetControllerType() => null;
        public bool SupportCapture => false;
        public bool SupportPartiallyRefund => false;
        public bool SupportRefund => false;
        public bool SupportVoid => false;
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
        public bool SkipPaymentInfo => false;
        public string PaymentMethodDescription => Description;
        public void Install() => throw new NotImplementedException();
        public void Uninstall() => throw new NotImplementedException();
        public Core.Configuration.PluginDescriptor PluginDescriptor { get; set; }
    }
}