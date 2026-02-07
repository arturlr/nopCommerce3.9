namespace Nop.Api8.Models;

public class AdminCustomerDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime LastActivityDateUtc { get; set; }
}

public class AdminCustomerListDto
{
    public List<AdminCustomerDto> Customers { get; set; } = new();
    public int TotalCount { get; set; }
}

public class UpdateCustomerRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool Active { get; set; }
}