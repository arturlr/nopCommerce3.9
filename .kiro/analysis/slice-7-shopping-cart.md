# Slice 7: Shopping Cart Operations Analysis

## Current Implementation Analysis

### ShoppingCartController Key Methods
- `AddProductToCart_Catalog` - AJAX add from catalog pages
- `AddProductToCart_Details` - AJAX add from product details
- `Cart` - Display cart page
- `UpdateCart` - Update quantities/remove items
- Core operations use `IShoppingCartService`

### ShoppingCartItem Entity Structure
- `Id` (primary key)
- `CustomerId` - Links to customer
- `ProductId` - Links to product
- `Quantity` - Item quantity
- `ShoppingCartTypeId` - Cart vs Wishlist (1=ShoppingCart, 2=Wishlist)
- `StoreId` - Multi-store support
- `CreatedOnUtc/UpdatedOnUtc` - Timestamps
- `AttributesXml` - Product attributes (defer for minimal)
- `CustomerEnteredPrice` - Custom pricing (defer for minimal)
- `RentalStartDateUtc/RentalEndDateUtc` - Rental products (defer for minimal)

### Key Services
- `IShoppingCartService` - Core cart operations
- `IProductService` - Product validation
- `IWorkContext` - Current customer context

## Minimal CRUD Requirements

### POST /api/v1/cart/items
- Add product to cart
- Required: CustomerId, ProductId, Quantity
- Basic validation: product exists, quantity > 0
- Return: Created cart item

### GET /api/v1/cart
- Get customer's cart items
- Required: CustomerId (from auth/header)
- Return: List of cart items with product details

### PUT /api/v1/cart/items/{id}
- Update cart item quantity
- Required: Quantity
- Validation: quantity > 0, item belongs to customer
- Return: Updated cart item

### DELETE /api/v1/cart/items/{id}
- Remove item from cart
- Validation: item belongs to customer
- Return: 204 No Content

## Deferred Features
- Product attributes/variants
- Custom pricing
- Rental products
- Shipping calculations
- Tax calculations
- Discounts/coupons
- Multi-store logic
- Wishlist operations