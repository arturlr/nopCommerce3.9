# Migration Startup Guide

This guide explains how to run both the original nopCommerce 3.90 (.NET Framework) and the new .NET 8 API together during the migration.

## Prerequisites

- .NET Framework 4.5.1+ (for original nopCommerce)
- .NET 8 SDK (for new API)
- SQL Server database

## Starting Both Applications

### 1. Start the .NET 8 API

```bash
# From the repository root
dotnet run --project src/Nop.Api8
```

The .NET 8 API will start on `http://localhost:5000`

### 2. Start the Original nopCommerce Application

```bash
# Build and run the original application (IIS Express or Visual Studio)
# Default URL: http://localhost:15536 (or your configured port)
```

## Feature Flag Configuration

### Enable .NET 8 API

Set the environment variable:
```bash
# Windows
set USE_DOTNET8_API=true

# Linux/Mac
export USE_DOTNET8_API=true
```

Or modify `src/Presentation/Nop.Web/.env`:
```
USE_DOTNET8_API=true
```

### Disable .NET 8 API (Fallback to Original)

```bash
# Windows
set USE_DOTNET8_API=false

# Linux/Mac
export USE_DOTNET8_API=false
```

Or modify `src/Presentation/Nop.Web/.env`:
```
USE_DOTNET8_API=false
```

## Verification Steps

### 1. Verify .NET 8 API is Running

```bash
# Test categories endpoint
curl http://localhost:5000/api/categories

# Test products endpoint  
curl http://localhost:5000/api/products
```

### 2. Verify Adapter Integration

1. Enable feature flag: `USE_DOTNET8_API=true`
2. Browse to category pages in the original nopCommerce app
3. Check logs for HTTP requests to .NET 8 API
4. Disable feature flag: `USE_DOTNET8_API=false`
5. Verify category pages still work (using original implementation)

### 3. Test Fallback Behavior

1. Enable feature flag: `USE_DOTNET8_API=true`
2. Stop the .NET 8 API (`Ctrl+C`)
3. Browse category pages - should still work using fallback
4. Check logs for fallback messages

## Troubleshooting

### .NET 8 API Not Starting
- Verify .NET 8 SDK is installed: `dotnet --version`
- Check database connection in `src/Nop.Api8/appsettings.json`

### Feature Flag Not Working
- Verify environment variable is set: `echo $USE_DOTNET8_API`
- Restart the nopCommerce application after changing the flag
- Check Autofac registration in DependencyRegistrar

### Adapter Not Being Used
- Verify HttpCatalogAdapter is registered as decorator
- Check that ICategoryService is being resolved through Autofac
- Enable debug logging to see HTTP requests

## Current Migration Status

- ✅ Product Catalog API (Read-only) - Categories and Products
- ⏳ Other features still use original implementation