# Slice 7: Shopping Cart API Design

## Endpoints

### POST /api/v1/cart/items
Add item to cart

**Request:**
```json
{
  "customerId": 123,
  "productId": 456,
  "quantity": 2
}
```

**Response (201):**
```json
{
  "id": 789,
  "customerId": 123,
  "productId": 456,
  "quantity": 2,
  "createdOnUtc": "2026-02-06T10:00:00Z",
  "updatedOnUtc": "2026-02-06T10:00:00Z"
}
```

### GET /api/v1/cart?customerId={id}
Get customer's cart

**Response (200):**
```json
{
  "customerId": 123,
  "items": [
    {
      "id": 789,
      "customerId": 123,
      "productId": 456,
      "productName": "Sample Product",
      "quantity": 2,
      "createdOnUtc": "2026-02-06T10:00:00Z",
      "updatedOnUtc": "2026-02-06T10:00:00Z"
    }
  ]
}
```

### PUT /api/v1/cart/items/{id}
Update cart item quantity

**Request:**
```json
{
  "quantity": 3
}
```

**Response (200):**
```json
{
  "id": 789,
  "customerId": 123,
  "productId": 456,
  "quantity": 3,
  "createdOnUtc": "2026-02-06T10:00:00Z",
  "updatedOnUtc": "2026-02-06T10:00:00Z"
}
```

### DELETE /api/v1/cart/items/{id}
Remove item from cart

**Response:** 204 No Content

## Error Responses

**400 Bad Request:**
```json
{
  "error": "Validation failed",
  "details": ["Quantity must be greater than 0"]
}
```

**404 Not Found:**
```json
{
  "error": "Cart item not found"
}
```