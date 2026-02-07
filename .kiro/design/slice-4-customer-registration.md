# Slice 4: Customer Registration API Design

## Endpoint Specification

### POST /api/v1/customers/register

**Purpose:** Register a new customer with minimal required information

**Request:**
- Method: POST
- Content-Type: application/json
- Body: CustomerRegistrationDto

**Response:**
- Success: 201 Created with CustomerDto
- Validation Error: 400 Bad Request with error details
- Duplicate Email: 409 Conflict with error message
- Server Error: 500 Internal Server Error

## Data Transfer Objects

### CustomerRegistrationDto (Request)
```csharp
public class CustomerRegistrationDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

### CustomerDto (Response)
```csharp
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegistrationDate { get; set; }
}
```

### ErrorResponseDto
```csharp
public class ErrorResponseDto
{
    public List<string> Errors { get; set; }
}
```

## Validation Rules

### Email
- Required
- Valid email format
- Unique (not already registered)
- Max length: 254 characters

### Password
- Required
- Minimum length: 6 characters
- No maximum length restriction (will be hashed)

### FirstName
- Required
- Max length: 100 characters
- Trimmed of whitespace

### LastName
- Required
- Max length: 100 characters
- Trimmed of whitespace

## Database Schema Requirements

### Customer Table
- Uses existing nopCommerce Customer table
- Email stored in Customer.Email
- Password stored in Customer.Password (hashed)
- FirstName/LastName stored as generic attributes

### Generic Attributes
- SystemCustomerAttributeNames.FirstName
- SystemCustomerAttributeNames.LastName

## Error Handling Strategy

### Validation Errors (400)
- Invalid email format
- Missing required fields
- Field length violations
- Password too short

### Business Logic Errors (409)
- Email already exists
- Registration disabled

### Server Errors (500)
- Database connection issues
- Unexpected exceptions
- Transaction rollback failures

## Transaction Management

### Registration Process
1. Begin transaction
2. Validate input data
3. Check email uniqueness
4. Create Customer record
5. Hash and store password
6. Save FirstName/LastName attributes
7. Commit transaction
8. Return success response

### Rollback Scenarios
- Any validation failure
- Database constraint violations
- Unexpected exceptions during save operations

## Security Considerations

### Password Handling
- Never log passwords
- Hash using nopCommerce's default password format
- Clear password from memory after hashing

### Input Sanitization
- Trim whitespace from all string inputs
- Validate email format server-side
- Prevent SQL injection through parameterized queries

### Rate Limiting
- Consider implementing rate limiting for registration endpoint
- Prevent automated account creation abuse