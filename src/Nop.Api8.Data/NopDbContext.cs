using Microsoft.EntityFrameworkCore;
using Nop.Api8.Data.Entities;

namespace Nop.Api8.Data;

public class NopDbContext : DbContext
{
    public NopDbContext(DbContextOptions<NopDbContext> options) : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<ProductPicture> ProductPictures { get; set; }
    public DbSet<Picture> Pictures { get; set; }
    public DbSet<ProductSpecificationAttribute> ProductSpecificationAttributes { get; set; }
    public DbSet<SpecificationAttributeOption> SpecificationAttributeOptions { get; set; }
    public DbSet<SpecificationAttribute> SpecificationAttributes { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<GenericAttribute> GenericAttributes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ProductCategory relationship
        modelBuilder.Entity<ProductCategory>()
            .HasKey(pc => new { pc.ProductId, pc.CategoryId });
        
        modelBuilder.Entity<ProductCategory>()
            .HasOne(pc => pc.Product)
            .WithMany(p => p.ProductCategories)
            .HasForeignKey(pc => pc.ProductId);
        
        modelBuilder.Entity<ProductCategory>()
            .HasOne(pc => pc.Category)
            .WithMany()
            .HasForeignKey(pc => pc.CategoryId);

        // ProductPicture relationship
        modelBuilder.Entity<ProductPicture>()
            .HasOne(pp => pp.Product)
            .WithMany(p => p.ProductPictures)
            .HasForeignKey(pp => pp.ProductId);

        modelBuilder.Entity<ProductPicture>()
            .HasOne(pp => pp.Picture)
            .WithMany()
            .HasForeignKey(pp => pp.PictureId);

        // ProductSpecificationAttribute relationship
        modelBuilder.Entity<ProductSpecificationAttribute>()
            .HasOne(psa => psa.Product)
            .WithMany(p => p.ProductSpecificationAttributes)
            .HasForeignKey(psa => psa.ProductId);

        modelBuilder.Entity<ProductSpecificationAttribute>()
            .HasOne(psa => psa.SpecificationAttributeOption)
            .WithMany()
            .HasForeignKey(psa => psa.SpecificationAttributeOptionId);

        // SpecificationAttributeOption relationship
        modelBuilder.Entity<SpecificationAttributeOption>()
            .HasOne(sao => sao.SpecificationAttribute)
            .WithMany()
            .HasForeignKey(sao => sao.SpecificationAttributeId);

        // ProductReview relationship
        modelBuilder.Entity<ProductReview>()
            .HasOne(pr => pr.Product)
            .WithMany(p => p.ProductReviews)
            .HasForeignKey(pr => pr.ProductId);

        // Customer-GenericAttribute relationship
        modelBuilder.Entity<GenericAttribute>()
            .HasOne(ga => ga.Customer)
            .WithMany(c => c.GenericAttributes)
            .HasForeignKey(ga => ga.EntityId)
            .HasPrincipalKey(c => c.Id);

        // Table mappings for existing nopCommerce schema
        modelBuilder.Entity<ProductPicture>().ToTable("Product_Picture_Mapping");
        modelBuilder.Entity<Picture>().ToTable("Picture");
        modelBuilder.Entity<ProductSpecificationAttribute>().ToTable("Product_SpecificationAttribute_Mapping");
        modelBuilder.Entity<SpecificationAttributeOption>().ToTable("SpecificationAttributeOption");
        modelBuilder.Entity<SpecificationAttribute>().ToTable("SpecificationAttribute");
        modelBuilder.Entity<ProductReview>().ToTable("ProductReview");
    }
}