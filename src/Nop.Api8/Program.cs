using Microsoft.EntityFrameworkCore;
using Nop.Api8.Data;
using Nop.Api8.Models;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGet("/api/v1/categories/{id:int}", async (int id, NopDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    return category == null ? Results.NotFound() : Results.Ok(new CategoryDto
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        SeName = category.SeName
    });
});

app.MapGet("/api/v1/categories/{id:int}/products", async (int id, NopDbContext db,
    int pageNumber = 1, int pageSize = 6, string orderBy = "position",
    decimal? priceMin = null, decimal? priceMax = null, bool? featuredOnly = null) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category == null) return Results.NotFound();

    var query = db.Products
        .Where(p => p.ProductCategories.Any(pc => pc.CategoryId == id));

    if (priceMin.HasValue)
        query = query.Where(p => p.Price >= priceMin.Value);
    if (priceMax.HasValue)
        query = query.Where(p => p.Price <= priceMax.Value);
    if (featuredOnly == true)
        query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == id && pc.IsFeaturedProduct));

    query = orderBy.ToLower() switch
    {
        "name" => query.OrderBy(p => p.Name),
        "price" => query.OrderBy(p => p.Price),
        "created" => query.OrderByDescending(p => p.CreatedOnUtc),
        _ => query.OrderBy(p => p.ProductCategories.First(pc => pc.CategoryId == id).DisplayOrder)
    };

    var totalItems = await query.CountAsync();
    var products = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Include(p => p.ProductCategories.Where(pc => pc.CategoryId == id))
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            Sku = p.Sku,
            Price = p.Price,
            IsFeatured = p.ProductCategories.Any(pc => pc.CategoryId == id && pc.IsFeaturedProduct)
        })
        .ToListAsync();

    return Results.Ok(new CategoryProductsDto
    {
        CategoryId = id,
        CategoryName = category.Name,
        Products = products,
        Pagination = new PaginationMetadata
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        }
    });
});

app.MapGet("/api/v1/products/{id:int}", async (int id, NopDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    return product == null ? Results.NotFound() : Results.Ok(new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        ShortDescription = product.ShortDescription,
        Sku = product.Sku,
        Price = product.Price
    });
});

app.MapGet("/api/v1/products/{id:int}/details", async (int id, NopDbContext db) =>
{
    var product = await db.Products
        .Include(p => p.ProductPictures)
            .ThenInclude(pp => pp.Picture)
        .Include(p => p.ProductSpecificationAttributes.Where(psa => psa.ShowOnProductPage))
            .ThenInclude(psa => psa.SpecificationAttributeOption)
                .ThenInclude(sao => sao.SpecificationAttribute)
        .Include(p => p.ProductReviews.Where(pr => pr.IsApproved))
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product == null)
        return Results.NotFound();

    var dto = new ProductDetailsDto
    {
        Id = product.Id,
        Name = product.Name,
        ShortDescription = product.ShortDescription,
        FullDescription = product.FullDescription,
        Sku = product.Sku,
        Price = product.Price,
        Images = product.ProductPictures
            .OrderBy(pp => pp.DisplayOrder)
            .Select(pp => new ProductImageDto
            {
                Url = $"/images/{pp.Picture.Id}/{pp.Picture.SeoFilename}",
                AltText = pp.Picture.AltAttribute
            }).ToArray(),
        Specifications = product.ProductSpecificationAttributes
            .OrderBy(psa => psa.DisplayOrder)
            .Select(psa => new ProductSpecificationDto
            {
                Name = psa.SpecificationAttributeOption.SpecificationAttribute.Name,
                Value = !string.IsNullOrEmpty(psa.CustomValue) 
                    ? psa.CustomValue 
                    : psa.SpecificationAttributeOption.Name
            }).ToArray(),
        ReviewsSummary = new ProductReviewsSummaryDto
        {
            TotalReviews = product.ProductReviews.Count,
            AverageRating = product.ProductReviews.Any() 
                ? (decimal)product.ProductReviews.Average(pr => pr.Rating) 
                : 0
        }
    };

    return Results.Ok(dto);
});

app.MapGet("/api/v1/products/search", async (NopDbContext db,
    string? q = null, int? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null,
    int pageNumber = 1, int pageSize = 10) =>
{
    var query = db.Products.AsQueryable();

    // Text search in Name, ShortDescription, FullDescription
    if (!string.IsNullOrWhiteSpace(q))
    {
        var searchTerm = q.Trim();
        query = query.Where(p => 
            p.Name.Contains(searchTerm) || 
            p.ShortDescription.Contains(searchTerm) || 
            p.FullDescription.Contains(searchTerm));
    }

    // Category filter
    if (categoryId.HasValue)
    {
        query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId.Value));
    }

    // Price filters
    if (minPrice.HasValue)
        query = query.Where(p => p.Price >= minPrice.Value);
    if (maxPrice.HasValue)
        query = query.Where(p => p.Price <= maxPrice.Value);

    // Only published, non-deleted products
    query = query.Where(p => p.Published && !p.Deleted);

    // Order by name
    query = query.OrderBy(p => p.Name);

    var totalItems = await query.CountAsync();
    var products = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Category)
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            Sku = p.Sku,
            Price = p.Price
        })
        .ToListAsync();

    var response = new ProductSearchDto
    {
        Products = products.ToArray(),
        Pagination = new PaginationDto
        {
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        },
        SearchQuery = q ?? string.Empty,
        AppliedFilters = new SearchFiltersDto
        {
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        }
    };

    return Results.Ok(response);
});

app.MapPost("/api/v1/customers/register", async (CustomerRegistrationDto request, NopDbContext db) =>
{
    try
    {
        // Basic validation
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email is required");
        else if (!IsValidEmail(request.Email))
            errors.Add("Invalid email format");
            
        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Password is required");
        else if (request.Password.Length < 6)
            errors.Add("Password must be at least 6 characters");
            
        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("First name is required");
            
        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("Last name is required");

        if (errors.Any())
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = errors });
        }

        // Trim inputs
        request.Email = request.Email.Trim();
        request.FirstName = request.FirstName.Trim();
        request.LastName = request.LastName.Trim();

        // Check if email already exists
        var existingCustomer = await db.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == request.Email.ToLower());
        
        if (existingCustomer != null)
        {
            return Results.Conflict(new ErrorResponseDto 
            { 
                Errors = ["Email already exists"] 
            });
        }

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // Create customer
            var customer = new Nop.Api8.Data.Entities.Customer
            {
                Email = request.Email,
                Username = request.Email, // Use email as username
                Password = HashPassword(request.Password), // Simple hash for now
                PasswordFormatId = 1, // Hashed
                CustomerGuid = Guid.NewGuid(),
                Active = true,
                Deleted = false,
                IsSystemAccount = false,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
                RegisteredInStoreId = 1 // Default store
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            // Save FirstName and LastName as generic attributes
            var firstNameAttr = new Nop.Api8.Data.Entities.GenericAttribute
            {
                EntityId = customer.Id,
                KeyGroup = "Customer",
                Key = "FirstName",
                Value = request.FirstName,
                StoreId = 0
            };

            var lastNameAttr = new Nop.Api8.Data.Entities.GenericAttribute
            {
                EntityId = customer.Id,
                KeyGroup = "Customer", 
                Key = "LastName",
                Value = request.LastName,
                StoreId = 0
            };

            db.GenericAttributes.Add(firstNameAttr);
            db.GenericAttributes.Add(lastNameAttr);
            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = new CustomerDto
            {
                CustomerId = customer.Id,
                Email = customer.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = customer.Active,
                RegistrationDate = customer.CreatedOnUtc
            };

            return Results.Created($"/api/v1/customers/{customer.Id}", response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Results.Problem("Registration failed: " + ex.Message);
        }
    }
    catch (Exception ex)
    {
        return Results.Problem("Registration failed: " + ex.Message);
    }
});

app.MapPut("/api/v1/customers/{id:int}", async (int id, CustomerUpdateDto request, NopDbContext db) =>
{
    try
    {
        // Basic validation
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email is required");
        else if (!IsValidEmail(request.Email))
            errors.Add("Invalid email format");
            
        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("First name is required");
            
        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("Last name is required");

        if (errors.Any())
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = errors });
        }

        // Trim inputs
        request.Email = request.Email.Trim();
        request.FirstName = request.FirstName.Trim();
        request.LastName = request.LastName.Trim();

        // Find customer
        var customer = await db.Customers.FindAsync(id);
        if (customer == null)
        {
            return Results.NotFound();
        }

        // Check if email already exists for another customer
        var existingCustomer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id != id && c.Email.ToLower() == request.Email.ToLower());
        
        if (existingCustomer != null)
        {
            return Results.Conflict(new ErrorResponseDto 
            { 
                Errors = ["Email already exists"] 
            });
        }

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // Update email
            customer.Email = request.Email;
            customer.Username = request.Email; // Keep username in sync
            
            // Update FirstName attribute
            var firstNameAttr = await db.GenericAttributes
                .FirstOrDefaultAsync(ga => ga.EntityId == id && ga.KeyGroup == "Customer" && ga.Key == "FirstName");
            if (firstNameAttr != null)
            {
                firstNameAttr.Value = request.FirstName;
            }
            else
            {
                db.GenericAttributes.Add(new Nop.Api8.Data.Entities.GenericAttribute
                {
                    EntityId = id,
                    KeyGroup = "Customer",
                    Key = "FirstName",
                    Value = request.FirstName,
                    StoreId = 0
                });
            }

            // Update LastName attribute
            var lastNameAttr = await db.GenericAttributes
                .FirstOrDefaultAsync(ga => ga.EntityId == id && ga.KeyGroup == "Customer" && ga.Key == "LastName");
            if (lastNameAttr != null)
            {
                lastNameAttr.Value = request.LastName;
            }
            else
            {
                db.GenericAttributes.Add(new Nop.Api8.Data.Entities.GenericAttribute
                {
                    EntityId = id,
                    KeyGroup = "Customer",
                    Key = "LastName",
                    Value = request.LastName,
                    StoreId = 0
                });
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = new CustomerDto
            {
                CustomerId = customer.Id,
                Email = customer.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = customer.Active,
                RegistrationDate = customer.CreatedOnUtc
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Results.Problem("Update failed: " + ex.Message);
        }
    }
    catch (Exception ex)
    {
        return Results.Problem("Update failed: " + ex.Message);
    }
});

// Shopping Cart Endpoints

app.MapPost("/api/v1/cart/items", async (CartItemRequestDto request, NopDbContext db) =>
{
    try
    {
        // Validate customer exists
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Customer not found"] });
        }

        // Validate product exists
        var product = await db.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Product not found"] });
        }

        // Check if item already exists in cart
        var existingItem = await db.ShoppingCartItems
            .FirstOrDefaultAsync(sci => sci.CustomerId == request.CustomerId 
                && sci.ProductId == request.ProductId 
                && sci.ShoppingCartTypeId == 1); // 1 = ShoppingCart

        if (existingItem != null)
        {
            // Update quantity
            existingItem.Quantity += request.Quantity;
            existingItem.UpdatedOnUtc = DateTime.UtcNow;
        }
        else
        {
            // Create new cart item
            var cartItem = new Nop.Api8.Data.Entities.ShoppingCartItem
            {
                CustomerId = request.CustomerId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                ShoppingCartTypeId = 1, // 1 = ShoppingCart
                StoreId = 1, // Default store
                AttributesXml = string.Empty,
                CustomerEnteredPrice = 0,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            db.ShoppingCartItems.Add(cartItem);
            existingItem = cartItem;
        }

        await db.SaveChangesAsync();

        var response = new CartItemDto
        {
            Id = existingItem.Id,
            CustomerId = existingItem.CustomerId,
            ProductId = existingItem.ProductId,
            ProductName = product.Name,
            Quantity = existingItem.Quantity,
            CreatedOnUtc = existingItem.CreatedOnUtc,
            UpdatedOnUtc = existingItem.UpdatedOnUtc
        };

        return Results.Created($"/api/v1/cart/items/{existingItem.Id}", response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to add item to cart: " + ex.Message);
    }
});

app.MapGet("/api/v1/cart", async (int customerId, NopDbContext db) =>
{
    try
    {
        var cartItems = await db.ShoppingCartItems
            .Where(sci => sci.CustomerId == customerId && sci.ShoppingCartTypeId == 1) // 1 = ShoppingCart
            .Include(sci => sci.Product)
            .OrderBy(sci => sci.CreatedOnUtc)
            .Select(sci => new CartItemDto
            {
                Id = sci.Id,
                CustomerId = sci.CustomerId,
                ProductId = sci.ProductId,
                ProductName = sci.Product.Name,
                Quantity = sci.Quantity,
                CreatedOnUtc = sci.CreatedOnUtc,
                UpdatedOnUtc = sci.UpdatedOnUtc
            })
            .ToListAsync();

        var response = new CartDto
        {
            CustomerId = customerId,
            Items = cartItems
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get cart: " + ex.Message);
    }
});

app.MapPut("/api/v1/cart/items/{id:int}", async (int id, CartItemUpdateDto request, NopDbContext db) =>
{
    try
    {
        var cartItem = await db.ShoppingCartItems
            .Include(sci => sci.Product)
            .FirstOrDefaultAsync(sci => sci.Id == id);

        if (cartItem == null)
        {
            return Results.NotFound();
        }

        cartItem.Quantity = request.Quantity;
        cartItem.UpdatedOnUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var response = new CartItemDto
        {
            Id = cartItem.Id,
            CustomerId = cartItem.CustomerId,
            ProductId = cartItem.ProductId,
            ProductName = cartItem.Product.Name,
            Quantity = cartItem.Quantity,
            CreatedOnUtc = cartItem.CreatedOnUtc,
            UpdatedOnUtc = cartItem.UpdatedOnUtc
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to update cart item: " + ex.Message);
    }
});

app.MapDelete("/api/v1/cart/items/{id:int}", async (int id, NopDbContext db) =>
{
    try
    {
        var cartItem = await db.ShoppingCartItems.FindAsync(id);
        if (cartItem == null)
        {
            return Results.NotFound();
        }

        db.ShoppingCartItems.Remove(cartItem);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to remove cart item: " + ex.Message);
    }
});

// Wishlist Endpoints

app.MapPost("/api/v1/wishlist/items", async (WishlistItemRequestDto request, NopDbContext db) =>
{
    try
    {
        // Validate customer exists
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Customer not found"] });
        }

        // Validate product exists
        var product = await db.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Product not found"] });
        }

        // Check if item already exists in wishlist
        var existingItem = await db.ShoppingCartItems
            .FirstOrDefaultAsync(sci => sci.CustomerId == request.CustomerId 
                && sci.ProductId == request.ProductId 
                && sci.ShoppingCartTypeId == 2); // 2 = Wishlist

        if (existingItem != null)
        {
            // Update quantity
            existingItem.Quantity += request.Quantity;
            existingItem.UpdatedOnUtc = DateTime.UtcNow;
        }
        else
        {
            // Create new wishlist item
            var wishlistItem = new Nop.Api8.Data.Entities.ShoppingCartItem
            {
                CustomerId = request.CustomerId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                ShoppingCartTypeId = 2, // 2 = Wishlist
                StoreId = 1, // Default store
                AttributesXml = string.Empty,
                CustomerEnteredPrice = 0,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            db.ShoppingCartItems.Add(wishlistItem);
            existingItem = wishlistItem;
        }

        await db.SaveChangesAsync();

        var response = new WishlistItemDto
        {
            Id = existingItem.Id,
            CustomerId = existingItem.CustomerId,
            ProductId = existingItem.ProductId,
            ProductName = product.Name,
            Quantity = existingItem.Quantity,
            CreatedOnUtc = existingItem.CreatedOnUtc,
            UpdatedOnUtc = existingItem.UpdatedOnUtc
        };

        return Results.Created($"/api/v1/wishlist/items/{existingItem.Id}", response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to add item to wishlist: " + ex.Message);
    }
});

app.MapGet("/api/v1/wishlist", async (int customerId, NopDbContext db) =>
{
    try
    {
        var wishlistItems = await db.ShoppingCartItems
            .Where(sci => sci.CustomerId == customerId && sci.ShoppingCartTypeId == 2) // 2 = Wishlist
            .Include(sci => sci.Product)
            .OrderBy(sci => sci.CreatedOnUtc)
            .Select(sci => new WishlistItemDto
            {
                Id = sci.Id,
                CustomerId = sci.CustomerId,
                ProductId = sci.ProductId,
                ProductName = sci.Product.Name,
                Quantity = sci.Quantity,
                CreatedOnUtc = sci.CreatedOnUtc,
                UpdatedOnUtc = sci.UpdatedOnUtc
            })
            .ToListAsync();

        var response = new WishlistDto
        {
            CustomerId = customerId,
            Items = wishlistItems
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get wishlist: " + ex.Message);
    }
});

app.MapPut("/api/v1/wishlist/items/{id:int}", async (int id, WishlistItemUpdateDto request, NopDbContext db) =>
{
    try
    {
        var wishlistItem = await db.ShoppingCartItems
            .Include(sci => sci.Product)
            .FirstOrDefaultAsync(sci => sci.Id == id);

        if (wishlistItem == null)
        {
            return Results.NotFound();
        }

        wishlistItem.Quantity = request.Quantity;
        wishlistItem.UpdatedOnUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var response = new WishlistItemDto
        {
            Id = wishlistItem.Id,
            CustomerId = wishlistItem.CustomerId,
            ProductId = wishlistItem.ProductId,
            ProductName = wishlistItem.Product.Name,
            Quantity = wishlistItem.Quantity,
            CreatedOnUtc = wishlistItem.CreatedOnUtc,
            UpdatedOnUtc = wishlistItem.UpdatedOnUtc
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to update wishlist item: " + ex.Message);
    }
});

app.MapDelete("/api/v1/wishlist/items/{id:int}", async (int id, NopDbContext db) =>
{
    try
    {
        var wishlistItem = await db.ShoppingCartItems.FindAsync(id);
        if (wishlistItem == null)
        {
            return Results.NotFound();
        }

        db.ShoppingCartItems.Remove(wishlistItem);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to remove wishlist item: " + ex.Message);
    }
});

// Product Review Endpoints

app.MapPost("/api/v1/products/{productId:int}/reviews", async (int productId, ProductReviewRequestDto request, NopDbContext db) =>
{
    try
    {
        // Validate product exists
        var product = await db.Products.FindAsync(productId);
        if (product == null)
        {
            return Results.NotFound();
        }

        // Validate customer exists
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Customer not found"] });
        }

        // Basic validation
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Title is required");
        if (string.IsNullOrWhiteSpace(request.ReviewText))
            errors.Add("Review text is required");
        if (request.Rating < 1 || request.Rating > 5)
            errors.Add("Rating must be between 1 and 5");

        if (errors.Any())
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = errors });
        }

        var review = new Nop.Api8.Data.Entities.ProductReview
        {
            ProductId = productId,
            CustomerId = request.CustomerId,
            Title = request.Title.Trim(),
            ReviewText = request.ReviewText.Trim(),
            Rating = request.Rating,
            IsApproved = true, // Auto-approve for simplicity
            CreatedOnUtc = DateTime.UtcNow
        };

        db.ProductReviews.Add(review);
        await db.SaveChangesAsync();

        var response = new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            CustomerId = review.CustomerId,
            Title = review.Title,
            ReviewText = review.ReviewText,
            Rating = review.Rating,
            IsApproved = review.IsApproved,
            CreatedOnUtc = review.CreatedOnUtc
        };

        return Results.Created($"/api/v1/products/{productId}/reviews/{review.Id}", response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to create review: " + ex.Message);
    }
});

app.MapGet("/api/v1/products/{productId:int}/reviews", async (int productId, NopDbContext db,
    int pageNumber = 1, int pageSize = 10) =>
{
    try
    {
        var product = await db.Products.FindAsync(productId);
        if (product == null)
        {
            return Results.NotFound();
        }

        var query = db.ProductReviews
            .Where(pr => pr.ProductId == productId && pr.IsApproved)
            .OrderByDescending(pr => pr.CreatedOnUtc);

        var totalItems = await query.CountAsync();
        var reviews = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(pr => new ProductReviewDto
            {
                Id = pr.Id,
                ProductId = pr.ProductId,
                CustomerId = pr.CustomerId,
                Title = pr.Title,
                ReviewText = pr.ReviewText,
                Rating = pr.Rating,
                IsApproved = pr.IsApproved,
                CreatedOnUtc = pr.CreatedOnUtc
            })
            .ToListAsync();

        var response = new ProductReviewsDto
        {
            ProductId = productId,
            Reviews = reviews.ToArray(),
            Pagination = new PaginationMetadata
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            }
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get reviews: " + ex.Message);
    }
});

// Checkout Endpoints

app.MapPost("/api/v1/checkout/validate", async (int customerId, NopDbContext db) =>
{
    try
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Customer not found"] });
        }

        var cartItems = await db.ShoppingCartItems
            .Where(sci => sci.CustomerId == customerId && sci.ShoppingCartTypeId == 1) // 1 = ShoppingCart
            .Include(sci => sci.Product)
            .ToListAsync();

        var errors = new List<string>();
        var total = 0m;

        if (!cartItems.Any())
        {
            errors.Add("Cart is empty");
        }
        else
        {
            foreach (var item in cartItems)
            {
                if (item.Product == null)
                {
                    errors.Add($"Product not found for cart item {item.Id}");
                    continue;
                }

                if (!item.Product.Published)
                {
                    errors.Add($"Product '{item.Product.Name}' is no longer available");
                }

                total += item.Product.Price * item.Quantity;
            }
        }

        var response = new CheckoutValidationDto
        {
            CustomerId = customerId,
            IsValid = !errors.Any(),
            Errors = errors,
            Total = total,
            ItemCount = cartItems.Sum(ci => ci.Quantity)
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Validation failed: " + ex.Message);
    }
});

app.MapPost("/api/v1/checkout/complete", async (CheckoutCompleteRequestDto request, NopDbContext db) =>
{
    using var transaction = await db.Database.BeginTransactionAsync();
    try
    {
        // Validate customer
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Customer not found"] });
        }

        // Get cart items
        var cartItems = await db.ShoppingCartItems
            .Where(sci => sci.CustomerId == request.CustomerId && sci.ShoppingCartTypeId == 1)
            .Include(sci => sci.Product)
            .ToListAsync();

        if (!cartItems.Any())
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Cart is empty"] });
        }

        // Calculate totals
        var subtotal = cartItems.Sum(ci => ci.Product.Price * ci.Quantity);
        var tax = subtotal * 0.1m; // Simple 10% tax
        var total = subtotal + tax;

        // Create order
        var order = new Nop.Api8.Data.Entities.Order
        {
            OrderGuid = Guid.NewGuid(),
            StoreId = 1,
            CustomerId = request.CustomerId,
            BillingAddressId = request.BillingAddressId,
            ShippingAddressId = request.ShippingAddressId,
            OrderStatusId = 10, // Pending
            PaymentStatusId = 10, // Pending
            ShippingStatusId = 10, // NotYetShipped
            CustomerCurrencyCode = "USD",
            CurrencyRate = 1.0m,
            OrderSubtotalExclTax = subtotal,
            OrderSubtotalInclTax = subtotal,
            OrderShippingExclTax = 0,
            OrderShippingInclTax = 0,
            OrderTax = tax,
            OrderTotal = total,
            PaymentMethodSystemName = "Manual",
            CreatedOnUtc = DateTime.UtcNow,
            Deleted = false
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(); // Save to get order ID

        // Create order items
        foreach (var cartItem in cartItems)
        {
            var orderItem = new Nop.Api8.Data.Entities.OrderItem
            {
                OrderItemGuid = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPriceExclTax = cartItem.Product.Price,
                UnitPriceInclTax = cartItem.Product.Price,
                PriceExclTax = cartItem.Product.Price * cartItem.Quantity,
                PriceInclTax = cartItem.Product.Price * cartItem.Quantity,
                DiscountAmountExclTax = 0,
                DiscountAmountInclTax = 0,
                AttributeDescription = string.Empty,
                AttributesXml = string.Empty
            };

            db.OrderItems.Add(orderItem);
        }

        // Clear cart
        db.ShoppingCartItems.RemoveRange(cartItems);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var response = new CheckoutCompleteResponseDto
        {
            OrderId = order.Id,
            OrderGuid = order.OrderGuid,
            OrderTotal = order.OrderTotal,
            OrderStatus = "Pending",
            CreatedOnUtc = order.CreatedOnUtc
        };

        return Results.Created($"/api/v1/orders/{order.Id}", response);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return Results.Problem("Checkout failed: " + ex.Message);
    }
});

// Order Management Endpoints

app.MapGet("/api/v1/orders/{id:int}", async (int id, NopDbContext db) =>
{
    try
    {
        var order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && !o.Deleted);

        if (order == null)
        {
            return Results.NotFound();
        }

        var response = new OrderDto
        {
            Id = order.Id,
            OrderGuid = order.OrderGuid,
            CustomerId = order.CustomerId,
            OrderStatusId = order.OrderStatusId,
            OrderTotal = order.OrderTotal,
            CustomerCurrencyCode = order.CustomerCurrencyCode,
            CreatedOnUtc = order.CreatedOnUtc,
            OrderItems = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product.Name,
                Quantity = oi.Quantity,
                UnitPriceInclTax = oi.UnitPriceInclTax,
                PriceInclTax = oi.PriceInclTax
            }).ToList()
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get order: " + ex.Message);
    }
});

app.MapGet("/api/v1/customers/{customerId:int}/orders", async (int customerId, NopDbContext db,
    int pageNumber = 1, int pageSize = 10) =>
{
    try
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer == null)
        {
            return Results.NotFound();
        }

        var query = db.Orders
            .Where(o => o.CustomerId == customerId && !o.Deleted)
            .OrderByDescending(o => o.CreatedOnUtc);

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderGuid = o.OrderGuid,
                CustomerId = o.CustomerId,
                OrderStatusId = o.OrderStatusId,
                OrderTotal = o.OrderTotal,
                CustomerCurrencyCode = o.CustomerCurrencyCode,
                CreatedOnUtc = o.CreatedOnUtc,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPriceInclTax = oi.UnitPriceInclTax,
                    PriceInclTax = oi.PriceInclTax
                }).ToList()
            })
            .ToListAsync();

        var response = new CustomerOrdersDto
        {
            Orders = orders,
            TotalCount = totalCount
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get customer orders: " + ex.Message);
    }
});

app.MapPut("/api/v1/orders/{id:int}/cancel", async (int id, NopDbContext db) =>
{
    try
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.Deleted);
        if (order == null)
        {
            return Results.NotFound();
        }

        // Only allow cancellation if order is pending
        if (order.OrderStatusId != 10) // 10 = Pending
        {
            return Results.BadRequest(new ErrorResponseDto 
            { 
                Errors = ["Order cannot be cancelled in current status"] 
            });
        }

        order.OrderStatusId = 40; // 40 = Cancelled
        await db.SaveChangesAsync();

        var response = new OrderDto
        {
            Id = order.Id,
            OrderGuid = order.OrderGuid,
            CustomerId = order.CustomerId,
            OrderStatusId = order.OrderStatusId,
            OrderTotal = order.OrderTotal,
            CustomerCurrencyCode = order.CustomerCurrencyCode,
            CreatedOnUtc = order.CreatedOnUtc,
            OrderItems = new List<OrderItemDto>()
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to cancel order: " + ex.Message);
    }
});

// Simple email validation function
static bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}

// Admin Product Management Endpoints

app.MapPost("/api/v1/admin/products", async (CreateProductRequest request, NopDbContext db) =>
{
    try
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required");
        if (request.Price < 0)
            errors.Add("Price must be non-negative");

        if (errors.Any())
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = errors });
        }

        var product = new Nop.Api8.Data.Entities.Product
        {
            Name = request.Name.Trim(),
            ShortDescription = request.ShortDescription?.Trim() ?? string.Empty,
            FullDescription = request.FullDescription?.Trim() ?? string.Empty,
            Sku = request.Sku?.Trim() ?? string.Empty,
            Price = request.Price,
            Published = request.Published,
            Deleted = false,
            CreatedOnUtc = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var response = new AdminProductDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            Sku = product.Sku,
            Price = product.Price,
            Published = product.Published
        };

        return Results.Created($"/api/v1/admin/products/{product.Id}", response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to create product: " + ex.Message);
    }
});

app.MapPut("/api/v1/admin/products/{id:int}", async (int id, UpdateProductRequest request, NopDbContext db) =>
{
    try
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.Deleted);
        if (product == null)
        {
            return Results.NotFound();
        }

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required");
        if (request.Price < 0)
            errors.Add("Price must be non-negative");

        if (errors.Any())
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = errors });
        }

        product.Name = request.Name.Trim();
        product.ShortDescription = request.ShortDescription?.Trim() ?? string.Empty;
        product.FullDescription = request.FullDescription?.Trim() ?? string.Empty;
        product.Sku = request.Sku?.Trim() ?? string.Empty;
        product.Price = request.Price;
        product.Published = request.Published;

        await db.SaveChangesAsync();

        var response = new AdminProductDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            Sku = product.Sku,
            Price = product.Price,
            Published = product.Published
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to update product: " + ex.Message);
    }
});

app.MapDelete("/api/v1/admin/products/{id:int}", async (int id, NopDbContext db) =>
{
    try
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.Deleted);
        if (product == null)
        {
            return Results.NotFound();
        }

        product.Deleted = true;
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to delete product: " + ex.Message);
    }
});

// Admin Order Management Endpoints

app.MapGet("/api/v1/admin/orders", async (NopDbContext db,
    int? orderStatus = null, DateTime? startDate = null, DateTime? endDate = null,
    int pageNumber = 1, int pageSize = 20) =>
{
    try
    {
        var query = db.Orders.Where(o => !o.Deleted);

        if (orderStatus.HasValue)
            query = query.Where(o => o.OrderStatusId == orderStatus.Value);

        if (startDate.HasValue)
            query = query.Where(o => o.CreatedOnUtc >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.CreatedOnUtc <= endDate.Value);

        query = query.OrderByDescending(o => o.CreatedOnUtc);

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AdminOrderDto
            {
                Id = o.Id,
                OrderGuid = o.OrderGuid,
                CustomerId = o.CustomerId,
                OrderStatusId = o.OrderStatusId,
                OrderStatus = o.OrderStatusId == 10 ? "Pending" : 
                             o.OrderStatusId == 20 ? "Processing" : 
                             o.OrderStatusId == 30 ? "Complete" : 
                             o.OrderStatusId == 40 ? "Cancelled" : "Unknown",
                OrderTotal = o.OrderTotal,
                CustomerCurrencyCode = o.CustomerCurrencyCode,
                CreatedOnUtc = o.CreatedOnUtc
            })
            .ToListAsync();

        var response = new AdminOrderListDto
        {
            Orders = orders,
            TotalCount = totalCount
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get orders: " + ex.Message);
    }
});

app.MapPut("/api/v1/admin/orders/{id:int}/status", async (int id, UpdateOrderStatusRequest request, NopDbContext db) =>
{
    try
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.Deleted);
        if (order == null)
        {
            return Results.NotFound();
        }

        // Validate status transition
        if (!IsValidStatusTransition(order.OrderStatusId, request.OrderStatusId))
        {
            return Results.BadRequest(new ErrorResponseDto 
            { 
                Errors = [$"Invalid status transition from {GetOrderStatusName(order.OrderStatusId)} to {GetOrderStatusName(request.OrderStatusId)}"] 
            });
        }

        order.OrderStatusId = request.OrderStatusId;
        await db.SaveChangesAsync();

        var response = new AdminOrderDto
        {
            Id = order.Id,
            OrderGuid = order.OrderGuid,
            CustomerId = order.CustomerId,
            OrderStatusId = order.OrderStatusId,
            OrderStatus = GetOrderStatusName(order.OrderStatusId),
            OrderTotal = order.OrderTotal,
            CustomerCurrencyCode = order.CustomerCurrencyCode,
            CreatedOnUtc = order.CreatedOnUtc
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to update order status: " + ex.Message);
    }
});

// Admin Customer Management Endpoints

app.MapGet("/api/v1/admin/customers", async (NopDbContext db,
    string? search = null, int pageNumber = 1, int pageSize = 20) =>
{
    try
    {
        var query = db.Customers.Where(c => !c.Deleted && !c.IsSystemAccount);

        // Search by email or name
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(c => c.Email.ToLower().Contains(searchTerm) ||
                c.GenericAttributes.Any(ga => ga.KeyGroup == "Customer" && 
                    (ga.Key == "FirstName" || ga.Key == "LastName") && 
                    ga.Value.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync();
        
        var customers = await query
            .Include(c => c.GenericAttributes.Where(ga => ga.KeyGroup == "Customer" && (ga.Key == "FirstName" || ga.Key == "LastName")))
            .OrderByDescending(c => c.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCustomerDto
            {
                Id = c.Id,
                Email = c.Email,
                FirstName = c.GenericAttributes.FirstOrDefault(ga => ga.Key == "FirstName")!.Value ?? "",
                LastName = c.GenericAttributes.FirstOrDefault(ga => ga.Key == "LastName")!.Value ?? "",
                Active = c.Active,
                CreatedOnUtc = c.CreatedOnUtc,
                LastActivityDateUtc = c.LastActivityDateUtc
            })
            .ToListAsync();

        var response = new AdminCustomerListDto
        {
            Customers = customers,
            TotalCount = totalCount
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to retrieve customers: " + ex.Message);
    }
});

app.MapPut("/api/v1/admin/customers/{id:int}", async (int id, UpdateCustomerRequest request, NopDbContext db) =>
{
    try
    {
        var customer = await db.Customers
            .Include(c => c.GenericAttributes.Where(ga => ga.KeyGroup == "Customer"))
            .FirstOrDefaultAsync(c => c.Id == id && !c.Deleted);

        if (customer == null)
        {
            return Results.NotFound();
        }

        // Update email
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != customer.Email)
        {
            var existingCustomer = await db.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == request.Email.ToLower() && c.Id != id);
            if (existingCustomer != null)
            {
                return Results.BadRequest(new ErrorResponseDto { Errors = ["Email already exists"] });
            }
            customer.Email = request.Email.Trim();
            customer.Username = request.Email.Trim();
        }

        // Update active status
        customer.Active = request.Active;

        // Update FirstName
        var firstNameAttr = customer.GenericAttributes.FirstOrDefault(ga => ga.Key == "FirstName");
        if (firstNameAttr != null)
        {
            firstNameAttr.Value = request.FirstName.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            db.GenericAttributes.Add(new Nop.Api8.Data.Entities.GenericAttribute
            {
                EntityId = id,
                KeyGroup = "Customer",
                Key = "FirstName",
                Value = request.FirstName.Trim(),
                StoreId = 0
            });
        }

        // Update LastName
        var lastNameAttr = customer.GenericAttributes.FirstOrDefault(ga => ga.Key == "LastName");
        if (lastNameAttr != null)
        {
            lastNameAttr.Value = request.LastName.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            db.GenericAttributes.Add(new Nop.Api8.Data.Entities.GenericAttribute
            {
                EntityId = id,
                KeyGroup = "Customer",
                Key = "LastName",
                Value = request.LastName.Trim(),
                StoreId = 0
            });
        }

        await db.SaveChangesAsync();

        var response = new AdminCustomerDto
        {
            Id = customer.Id,
            Email = customer.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Active = customer.Active,
            CreatedOnUtc = customer.CreatedOnUtc,
            LastActivityDateUtc = customer.LastActivityDateUtc
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to update customer: " + ex.Message);
    }
});

app.MapDelete("/api/v1/admin/customers/{id:int}", async (int id, NopDbContext db) =>
{
    try
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.Deleted);
        if (customer == null)
        {
            return Results.NotFound();
        }

        // Soft delete - set Active = false
        customer.Active = false;
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to delete customer: " + ex.Message);
    }
});

// Payment Endpoints

app.MapGet("/api/v1/payments/methods", async (int? customerId, NopDbContext db) =>
{
    try
    {
        // For minimal implementation, return hardcoded payment methods
        // In real implementation, this would call the payment service
        var paymentMethods = new List<PaymentMethodDto>
        {
            new PaymentMethodDto
            {
                SystemName = "Payments.CheckMoneyOrder",
                FriendlyName = "Check / Money Order",
                Description = "Pay by check or money order",
                SkipPaymentInfo = false,
                PaymentMethodType = "Standard"
            },
            new PaymentMethodDto
            {
                SystemName = "Payments.Manual",
                FriendlyName = "Credit Card (Manual)",
                Description = "Credit card processed manually",
                SkipPaymentInfo = false,
                PaymentMethodType = "Standard"
            },
            new PaymentMethodDto
            {
                SystemName = "Payments.PurchaseOrder",
                FriendlyName = "Purchase Order",
                Description = "Pay by purchase order",
                SkipPaymentInfo = false,
                PaymentMethodType = "Standard"
            }
        };

        return Results.Ok(paymentMethods);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get payment methods: " + ex.Message);
    }
});

app.MapPost("/api/v1/payments/process", async (PaymentProcessRequestDto request, NopDbContext db) =>
{
    try
    {
        // Basic validation
        var errors = new List<string>();
        if (request.CustomerId <= 0)
            errors.Add("Customer ID is required");
        if (request.OrderTotal <= 0)
            errors.Add("Order total must be greater than zero");
        if (string.IsNullOrWhiteSpace(request.PaymentMethodSystemName))
            errors.Add("Payment method is required");

        if (errors.Any())
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = errors });
        }

        // Validate customer exists
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Customer not found"] });
        }

        // For minimal implementation, simulate payment processing
        // In real implementation, this would delegate to the actual payment plugin
        var result = new PaymentProcessResultDto
        {
            Success = true,
            AuthorizationTransactionId = Guid.NewGuid().ToString(),
            AuthorizationTransactionCode = "AUTH_" + DateTime.UtcNow.Ticks,
            AuthorizationTransactionResult = "Authorized",
            PaymentStatus = "Authorized",
            AllowStoringCreditCardNumber = false,
            AllowStoringDirectDebit = false
        };

        // Simulate different payment method behaviors
        switch (request.PaymentMethodSystemName)
        {
            case "Payments.CheckMoneyOrder":
                result.PaymentStatus = "Pending";
                result.AuthorizationTransactionResult = "Pending Check/Money Order";
                break;
            case "Payments.Manual":
                if (string.IsNullOrWhiteSpace(request.CreditCardNumber))
                {
                    result.Success = false;
                    result.ErrorMessage = "Credit card number is required";
                }
                break;
            case "Payments.PurchaseOrder":
                result.PaymentStatus = "Pending";
                result.AuthorizationTransactionResult = "Pending Purchase Order";
                break;
        }

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem("Payment processing failed: " + ex.Message);
    }
});

// Shipping Endpoints

app.MapGet("/api/v1/shipping/methods", async (int? countryId, NopDbContext db) =>
{
    try
    {
        // For minimal implementation, return hardcoded shipping methods
        // In real implementation, this would call the shipping service
        var shippingMethods = new List<ShippingMethodDto>
        {
            new ShippingMethodDto
            {
                Id = 1,
                Name = "Ground",
                Description = "Standard ground shipping",
                DisplayOrder = 1
            },
            new ShippingMethodDto
            {
                Id = 2,
                Name = "Next Day Air",
                Description = "Next day air delivery",
                DisplayOrder = 2
            },
            new ShippingMethodDto
            {
                Id = 3,
                Name = "2nd Day Air",
                Description = "Second day air delivery",
                DisplayOrder = 3
            }
        };

        return Results.Ok(shippingMethods);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get shipping methods: " + ex.Message);
    }
});

app.MapPost("/api/v1/shipping/calculate", async (ShippingRateRequestDto request, NopDbContext db) =>
{
    try
    {
        // Basic validation
        var errors = new List<string>();
        if (request.CustomerId <= 0)
            errors.Add("Customer ID is required");
        if (!request.Items.Any())
            errors.Add("Cart items are required");
        if (request.ShippingAddress == null)
            errors.Add("Shipping address is required");

        if (errors.Any())
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = errors });
        }

        // Validate customer exists
        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return Results.BadRequest(new ErrorResponseDto { Errors = ["Customer not found"] });
        }

        // For minimal implementation, simulate shipping rate calculation
        // In real implementation, this would delegate to shipping plugins
        var shippingOptions = new List<ShippingOptionDto>
        {
            new ShippingOptionDto
            {
                Name = "Ground",
                Description = "Standard ground shipping (5-7 business days)",
                Rate = 5.99m,
                ShippingRateComputationMethodSystemName = "Shipping.FixedOrByWeight"
            },
            new ShippingOptionDto
            {
                Name = "Next Day Air",
                Description = "Next day air delivery",
                Rate = 25.99m,
                ShippingRateComputationMethodSystemName = "Shipping.FixedOrByWeight"
            },
            new ShippingOptionDto
            {
                Name = "2nd Day Air",
                Description = "Second day air delivery",
                Rate = 15.99m,
                ShippingRateComputationMethodSystemName = "Shipping.FixedOrByWeight"
            }
        };

        // Simple weight-based calculation
        var totalWeight = 0m;
        foreach (var item in request.Items)
        {
            var product = await db.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                totalWeight += item.Quantity * 1.0m; // Assume 1 lb per item for simplicity
            }
        }

        // Adjust rates based on weight
        if (totalWeight > 10)
        {
            foreach (var option in shippingOptions)
            {
                option.Rate += (totalWeight - 10) * 0.5m; // $0.50 per lb over 10 lbs
            }
        }

        var result = new ShippingRateResultDto
        {
            ShippingOptions = shippingOptions,
            Success = true,
            Errors = new List<string>()
        };

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem("Shipping rate calculation failed: " + ex.Message);
    }
});

// Widget Endpoints

app.MapGet("/api/v1/widgets/zones", async () =>
{
    try
    {
        // Common widget zones in nopCommerce
        var widgetZones = new List<WidgetZoneDto>
        {
            new WidgetZoneDto
            {
                Name = "home_page_top",
                DisplayName = "Home Page Top",
                WidgetCount = 1 // Simulated count
            },
            new WidgetZoneDto
            {
                Name = "home_page_bottom",
                DisplayName = "Home Page Bottom",
                WidgetCount = 0
            },
            new WidgetZoneDto
            {
                Name = "left_side_column_before",
                DisplayName = "Left Side Column Before",
                WidgetCount = 0
            },
            new WidgetZoneDto
            {
                Name = "right_side_column_before",
                DisplayName = "Right Side Column Before",
                WidgetCount = 0
            },
            new WidgetZoneDto
            {
                Name = "content_before",
                DisplayName = "Content Before",
                WidgetCount = 0
            },
            new WidgetZoneDto
            {
                Name = "content_after",
                DisplayName = "Content After",
                WidgetCount = 0
            }
        };

        return Results.Ok(widgetZones);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get widget zones: " + ex.Message);
    }
});

app.MapGet("/api/v1/widgets/zone/{zoneName}", async (string zoneName) =>
{
    try
    {
        // For minimal implementation, simulate widgets for common zones
        var widgets = new List<WidgetDto>();

        if (zoneName == "home_page_top")
        {
            widgets.Add(new WidgetDto
            {
                SystemName = "Widgets.NivoSlider",
                FriendlyName = "Nivo Slider",
                Description = "Image slider for home page",
                WidgetZone = zoneName,
                ActionName = "PublicInfo",
                ControllerName = "WidgetsNivoSlider",
                IsActive = true
            });
        }

        var response = new WidgetZoneContentDto
        {
            ZoneName = zoneName,
            Widgets = widgets
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to get widgets for zone: " + ex.Message);
    }
});

app.Run("http://localhost:5000");

// Helper functions for order status
static string GetOrderStatusName(int statusId) => statusId switch
{
    10 => "Pending",
    20 => "Processing", 
    30 => "Complete",
    40 => "Cancelled",
    _ => "Unknown"
};

static bool IsValidStatusTransition(int currentStatus, int newStatus)
{
    return (currentStatus, newStatus) switch
    {
        (10, 20) => true, // Pending → Processing
        (10, 40) => true, // Pending → Cancelled
        (20, 30) => true, // Processing → Complete
        (20, 40) => true, // Processing → Cancelled
        _ => false
    };
}

// Simple password hashing function (minimal implementation)
static string HashPassword(string password)
{
    // For minimal implementation, use SHA256 (nopCommerce uses more sophisticated hashing)
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
    return Convert.ToBase64String(hashedBytes);
}

public partial class Program { }