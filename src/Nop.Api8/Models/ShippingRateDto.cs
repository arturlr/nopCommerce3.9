using System.Collections.Generic;

namespace Nop.Api8.Models
{
    public class ShippingRateRequestDto
    {
        public int CustomerId { get; set; }
        public List<ShippingCartItemDto> Items { get; set; } = new List<ShippingCartItemDto>();
        public ShippingAddressDto ShippingAddress { get; set; }
        public int StoreId { get; set; } = 1;
    }

    public class ShippingCartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ShippingAddressDto
    {
        public string Address1 { get; set; }
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string ZipPostalCode { get; set; }
        public int CountryId { get; set; }
    }

    public class ShippingRateResultDto
    {
        public List<ShippingOptionDto> ShippingOptions { get; set; } = new List<ShippingOptionDto>();
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ShippingOptionDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Rate { get; set; }
        public string ShippingRateComputationMethodSystemName { get; set; }
    }
}