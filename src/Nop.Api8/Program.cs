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

// Simple password hashing function (minimal implementation)
static string HashPassword(string password)
{
    // For minimal implementation, use SHA256 (nopCommerce uses more sophisticated hashing)
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
    return Convert.ToBase64String(hashedBytes);
}

app.Run("http://localhost:5000");

public partial class Program { }