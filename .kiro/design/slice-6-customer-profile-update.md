# Slice 6: Customer Profile Update API Design

## Endpoint
PUT /api/v1/customers/{id}

## Request DTO
```csharp
public class CustomerUpdateDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; }
}
```

## Response DTO
```csharp
public class CustomerDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

## Validation Rules
- Email: Required, valid email format, unique in system
- FirstName: Required, max 100 characters
- LastName: Required, max 100 characters

## Implementation Notes
- Update Customer.Email directly
- Update FirstName/LastName via GenericAttribute table
- Return 404 if customer not found
- Return 400 for validation errors
- Return 409 if email already exists for another customer