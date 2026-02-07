# Project Structure

## Solution Organization

The solution is organized into four main folders:

### Libraries/
Core business logic and data access layers.

- **Nop.Core**: Domain models, interfaces, infrastructure, and core utilities
  - `Domain/`: Entity classes organized by feature (Catalog, Customers, Orders, etc.)
  - `Infrastructure/`: DI container, engine, type finder, startup tasks
  - `Caching/`: Cache managers (Memory, Redis, PerRequest)
  - `Data/`: Data provider abstractions and settings
  - `Plugins/`: Plugin discovery and management
  - `Events/`: Domain event system

- **Nop.Data**: Entity Framework implementation
  - `Mapping/`: EF entity configurations
  - `Initializers/`: Database initialization strategies
  - `EfRepository.cs`: Generic repository implementation

- **Nop.Services**: Business logic and service layer
  - Organized by domain area (Catalog, Customers, Orders, Shipping, etc.)
  - Each area typically has service interfaces and implementations
  - Follows naming: `I{Feature}Service` and `{Feature}Service`

### Presentation/
Web application and admin interface.

- **Nop.Web**: Public-facing storefront (MVC application)
- **Nop.Admin**: Administration panel
- **Nop.Web.Framework**: Shared presentation layer utilities, filters, and base classes

### Plugins/
Extensible plugin modules for payments, shipping, widgets, etc.

- **Naming Convention**: `Nop.Plugin.{Category}.{Name}`
- **Categories**: Payments, Shipping, Tax, Widgets, DiscountRules, ExternalAuth, Feed, Pickup, ExchangeRate
- **Structure**: Each plugin has Controllers, Models, Views, and a main plugin class

### Tests/
Unit and integration tests.

- **Nop.Core.Tests**: Core library tests
- **Nop.Data.Tests**: Data layer tests
- **Nop.Services.Tests**: Service layer tests
- **Nop.Web.MVC.Tests**: Web layer tests
- **Nop.Tests**: Shared test utilities

## Key Conventions

### Naming
- **Entities**: PascalCase, inherit from `BaseEntity`
- **Interfaces**: Prefix with `I` (e.g., `IProductService`)
- **Services**: `{Domain}Service` pattern
- **Repositories**: Generic `IRepository<T>` pattern
- **Settings**: Suffix with `Settings` (e.g., `CatalogSettings`)

### Domain Models
- Located in `Nop.Core/Domain/{Feature}/`
- Inherit from `BaseEntity` (provides `Id` property)
- Use partial classes for extensibility
- Navigation properties use lazy initialization pattern
- Enum-backed properties expose both `{Property}Id` (int) and `{Property}` (enum)

### Data Access
- Repository pattern via `IRepository<T>`
- `Table` property for tracked queries
- `TableNoTracking` property for read-only queries
- Entity Framework handles change tracking and persistence

### Dependency Injection
- Register dependencies in `DependencyRegistrar` classes
- Use constructor injection
- Lifetime scopes: InstancePerLifetimeScope (per request), SingleInstance, InstancePerDependency

### Plugin Architecture
- Plugins implement `IPlugin` or derive from `BasePlugin`
- `Description.txt` contains plugin metadata
- Plugins can be dynamically loaded/unloaded
- Use `PluginDescriptor` for plugin information

## File Organization
- Configuration files: `app.config`, `web.config`, `packages.config`
- Each project has `Properties/AssemblyInfo.cs`
- Plugins include `logo.jpg` for visual identification
