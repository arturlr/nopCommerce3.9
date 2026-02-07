# Slice 3: Product Details Page - Analysis

## Overview
Enhance the existing GET /api/v1/products/{id} endpoint to include full product details: FullDescription, Images, Specifications, ReviewsSummary. Based on ProductController.ProductDetails logic.

## Current State Analysis

### Existing ProductController.ProductDetails Logic
- **Location**: `src/Presentation/Nop.Web/Controllers/ProductController.cs`
- **Key Method**: `ProductDetails(int productId, int updatecartitemid = 0)`
- **Model Factory**: `_productModelFactory.PrepareProductDetailsModel(product, updatecartitem, false)`

### Current ProductDetailsModel Structure
Key fields we need to add to our API:
- `FullDescription` - Full product description (HTML content)
- `PictureModels` - List of product images with URLs
- `ProductSpecifications` - List of specification attributes
- `ProductReviewOverview` - Review summary (rating, count)

### Current .NET 8 API ProductDto
Currently has: Id, Name, ShortDescription, Sku, Price, IsFeatured

## Implementation Plan

### Step 1: Enhanced ProductDetailsDto
Create new DTO with additional fields:
- `FullDescription` (string)
- `Images` (array of image objects with URL, alt text)
- `Specifications` (array of spec objects with name/value)
- `ReviewsSummary` (object with average rating, total reviews)

### Step 2: Database Entities
Need to map additional entities:
- `ProductPicture` - Product images relationship
- `Picture` - Image storage
- `ProductSpecificationAttribute` - Product specifications
- `SpecificationAttribute` - Specification definitions
- `ProductReview` - For review summary

### Step 3: API Endpoint
Enhance existing GET /api/v1/products/{id} or create new GET /api/v1/products/{id}/details

### Step 4: Adapter Integration
Update HttpCatalogAdapter to call new endpoint with fallback to existing logic

## Key Services from nopCommerce

### ProductModelFactory.PrepareProductDetailsModel
- Calls `PrepareProductDetailsPictureModel()` for images
- Calls `PrepareProductSpecificationModel()` for specifications  
- Calls `PrepareProductReviewOverviewModel()` for review summary
- Handles localization via `product.GetLocalized(x => x.FullDescription)`

### Image Handling
- Uses `IPictureService.GetPicturesByProductId()`
- Generates URLs via `IPictureService.GetPictureUrl()`
- Handles different image sizes

### Specifications
- Uses `ISpecificationAttributeService.GetProductSpecificationAttributes()`
- Groups by specification attribute
- Handles localization

### Reviews Summary
- Calculates from `product.ProductReviews` collection
- Filters by approved status and store
- Provides `RatingSum` and `TotalReviews`

## Minimal Implementation Scope

### Include:
- FullDescription (localized)
- Images (URLs only, primary image size)
- Basic specifications (name/value pairs)
- Review summary (average rating, total count)

### Defer:
- Multiple image sizes
- Complex specification grouping
- Individual review details
- Related products
- Product attributes/variants
- Tier pricing
- Inventory details

## Database Schema Requirements

### Tables Needed:
- `Product` (already mapped)
- `ProductPicture` (junction table)
- `Picture` (image storage)
- `ProductSpecificationAttribute` (junction table)
- `SpecificationAttribute` (specification definitions)
- `ProductReview` (for summary calculation)

## API Design

### Endpoint: GET /api/v1/products/{id}/details

Response structure:
```json
{
  "id": 1,
  "name": "Product Name",
  "shortDescription": "Short desc",
  "fullDescription": "Full HTML description",
  "sku": "SKU123",
  "price": 29.99,
  "images": [
    {
      "url": "https://example.com/image1.jpg",
      "altText": "Product image"
    }
  ],
  "specifications": [
    {
      "name": "Color",
      "value": "Red"
    }
  ],
  "reviewsSummary": {
    "averageRating": 4.5,
    "totalReviews": 23
  }
}
```

## Migration Strategy

1. Keep existing GET /api/v1/products/{id} for basic info
2. Add new GET /api/v1/products/{id}/details for full details
3. Update adapter to use appropriate endpoint based on needs
4. Maintain backward compatibility