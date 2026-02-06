# Slice 4: Customer Registration Analysis

## Current Implementation Analysis

### CustomerController.Register (GET/POST)

**GET Action:**
- Checks if registration is allowed (`_customerSettings.UserRegistrationType != UserRegistrationType.Disabled`)
- Creates `RegisterModel` with extensive fields via `_customerModelFactory.PrepareRegisterModel()`
- Returns registration view

**POST Action:**
- Validates CAPTCHA, honeypot, custom customer attributes
- Uses `ICustomerRegistrationService.RegisterCustomer()` for core registration
- Handles password hashing, duplicate email checks internally
- Saves extensive customer attributes (name, address, VAT, timezone, etc.)
- Supports different registration types (Standard, EmailValidation, AdminApproval)
- Creates default address, sends notifications, handles external auth association

### Key Services Used

**ICustomerRegistrationService:**
- `RegisterCustomer(CustomerRegistrationRequest)` - Core registration logic
- Handles password hashing, validation, duplicate checks
- Returns `CustomerRegistrationResult` with success/error status

**CustomerRegistrationRequest:**
- Customer, Email, Username, Password, PasswordFormat, StoreId, IsApproved

**Dependencies:**
- `ICustomerService` - Customer CRUD operations
- `IGenericAttributeService` - Store customer attributes
- `IAuthenticationService` - Sign in after registration
- `IWorkflowMessageService` - Send welcome/notification emails
- `IEventPublisher` - Raise CustomerRegisteredEvent

### Current Validation

**RegisterModel Fields:**
- Email (required, email format)
- Password/ConfirmPassword (required, matching)
- FirstName/LastName (optional by default)
- Username (if enabled)
- Extensive optional fields (address, company, phone, etc.)

**Built-in Validations:**
- Email format and uniqueness
- Password strength (via CustomerSettings)
- Username uniqueness (if enabled)
- Custom customer attributes validation

## Minimal API Design

### POST /api/v1/customers/register

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Success Response (201):**
```json
{
  "customerId": 123,
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "isActive": true,
  "registrationDate": "2026-02-06T10:30:00Z"
}
```

**Error Response (400):**
```json
{
  "errors": [
    "Email already exists",
    "Password must be at least 6 characters"
  ]
}
```

## Implementation Requirements

### Minimal Fields Only
- Email (required, unique)
- Password (required, hashed)
- FirstName (required)
- LastName (required)

### Core Validations
- Email format and uniqueness
- Password strength (minimum length)
- Required field validation
- Proper error handling with transaction rollback

### Database Operations
- Create Customer record
- Hash and store password
- Save FirstName/LastName as generic attributes
- Handle duplicate email gracefully

### Error Handling
- Validation errors (400 Bad Request)
- Duplicate email (409 Conflict)
- Server errors (500 Internal Server Error)
- Transaction rollback on failures

## Deferred Features
- External authentication (OAuth, social login)
- GDPR consent handling
- Custom customer attributes
- Address collection
- Newsletter subscription
- Email verification workflow
- Admin approval workflow
- CAPTCHA validation
- Complex password policies