---
name: dotnet-48-to-8-strangler-nop
description: Incremental migration of nopCommerce 3.90 (ASP.NET MVC 5, .NET Framework) towards .NET 8 using the Strangler Fig pattern, preserving existing architecture.
---

## Context

- Target repository: nopCommerce 3.90 (ASP.NET MVC, pluggable architecture, MS SQL).[file:1]
- Layers: Presentation\Nop.Web, Libraries\Nop.Core, Libraries\Nop.Services, Libraries\Nop.Data, Plugins\*.[file:1]
- Goal: Introduce .NET 8 components behind a strangler/proxy boundary while keeping the existing MVC + plugin architecture and folder structure as intact as possible.

## Technology Decisions for .NET 8 Components

### Framework & Runtime
- **Target Framework**: .NET 8.0 (LTS)
- **Web Framework**: ASP.NET Core 8.0 with Minimal APIs or Controllers (match existing MVC style for consistency)
- **Hosting**: Kestrel with IIS/nginx reverse proxy support

### Data Access
- **ORM**: Entity Framework Core 8.0
  - Migrate from EF6 incrementally per slice
  - Use same database schema initially (shared database strategy)
  - Consider EF Core Power Tools for reverse engineering existing schema
- **Database**: Continue with MS SQL Server (2019+)
- **Connection Strategy**: Shared connection string, separate DbContext per bounded context

### Dependency Injection
- **Primary**: Built-in ASP.NET Core DI (Microsoft.Extensions.DependencyInjection)
- **Migration Path**: 
  - Keep Autofac in .NET Framework side
  - Use Autofac.Extensions.DependencyInjection bridge if needed for gradual transition
  - Eventually standardize on built-in DI

### API & Communication
- **API Style**: RESTful HTTP APIs with JSON
- **Client**: HttpClient with IHttpClientFactory
- **Resilience**: Polly for retry, circuit breaker, timeout policies
- **Serialization**: System.Text.Json (with Newtonsoft.Json compatibility shim if needed)

### Authentication & Session
- **Auth**: 
  - Share authentication cookies between .NET Framework and .NET 8 (same machine key)
  - Use ASP.NET Core Data Protection with shared key storage
  - Maintain existing Forms Authentication initially, migrate to ASP.NET Core Identity per slice
- **Session**: 
  - Distributed cache (Redis) for session state sharing
  - Or use stateless JWT tokens for new endpoints

### Caching
- **Strategy**: Continue with existing Redis infrastructure (StackExchange.Redis)
- **Implementation**: Use IDistributedCache abstraction in .NET 8 components

### Logging & Monitoring
- **Logging**: Microsoft.Extensions.Logging with Serilog
- **Metrics**: OpenTelemetry for distributed tracing across both versions
- **Health Checks**: ASP.NET Core Health Checks for .NET 8 endpoints

## Principles

1. Preserve existing architecture and abstractions (controllers, services, repositories, plugin interfaces) unless change is strictly required by .NET 8.
2. Work in vertical slices (features) rather than broad cross-cutting refactors.
3. Always keep the system in a buildable, testable state.
4. Every step must be verified (build, tests, basic HTTP checks) before moving to the next step.
5. Record the status of each slice in migration-log.md.

## Slice Definition

A slice is one coherent feature, for example:

- Storefront: home page featured products, category listing, product details, search, cart, checkout.
- Admin: product list/edit, order list/edit, customer list/edit.
- Plugins: payment provider, shipping method, tax provider, external authentication.

## Slice Prioritization Criteria

### Selection Framework

Pick slices based on these weighted criteria:

1. **Coupling Level (High Priority)**
   - Low coupling: Features with minimal dependencies on other subsystems
   - Avoid features that touch many plugins or core services initially
   - Prefer features with clear boundaries

2. **Business Value (High Priority)**
   - Customer-facing features that improve performance or user experience
   - Features with known technical debt or performance issues
   - High-traffic endpoints that benefit from .NET 8 performance improvements

3. **Risk Assessment (Medium Priority)**
   - Start with read-only or read-heavy features (lower risk)
   - Avoid critical payment/checkout flows initially
   - Consider data consistency requirements

4. **Technical Complexity (Medium Priority)**
   - Simple CRUD operations before complex workflows
   - Features with existing test coverage
   - Avoid plugin-heavy features early on

5. **Plugin Independence (Low Priority)**
   - Features that don't rely heavily on plugin extensibility
   - Or features where plugin contracts can be easily adapted

### Recommended Migration Order

#### Phase 1: Foundation & Read-Only Features (Weeks 1-4)
1. **Product Catalog API** (Read-only)
   - GET /api/products, /api/categories
   - Low risk, high value, establishes patterns
   - Tests adapter pattern and data access

2. **Category Listing** (Read-only)
   - Storefront category pages
   - Minimal business logic, good for learning

3. **Product Details Page** (Read-only)
   - Single product view
   - Tests caching strategy

#### Phase 2: Simple Write Operations (Weeks 5-8)
4. **Customer Registration**
   - POST /api/customers/register
   - Tests authentication sharing
   - Relatively isolated feature

5. **Product Search**
   - GET /api/search
   - Tests performance improvements
   - Can run A/B comparison

6. **Customer Profile Updates**
   - PUT /api/customers/profile
   - Simple write operation with validation

#### Phase 3: Complex Workflows (Weeks 9-16)
7. **Shopping Cart Operations**
   - Add/remove/update cart items
   - Tests session sharing
   - Critical but well-defined boundaries

8. **Wishlist Management**
   - Similar to cart but lower risk

9. **Product Reviews**
   - Write operations with moderation workflow

#### Phase 4: Critical Paths (Weeks 17-24)
10. **Checkout Process** (High risk - careful planning)
    - Multi-step workflow
    - Payment gateway integration
    - Requires extensive testing

11. **Order Management**
    - Order creation, updates, status changes
    - Admin and customer views

#### Phase 5: Admin Features (Weeks 25-32)
12. **Admin Product Management**
    - CRUD operations for products
    - Bulk operations
    - Image uploads

13. **Admin Order Management**
    - Order processing workflows
    - Refunds, cancellations

14. **Admin Customer Management**
    - Customer CRUD
    - Role assignments

#### Phase 6: Plugin-Heavy Features (Weeks 33+)
15. **Payment Providers**
    - Migrate plugin contracts
    - One provider at a time

16. **Shipping Methods**
    - Plugin-based shipping calculations

17. **Tax Providers**
    - Plugin-based tax calculations

### Slice Validation Checklist

Before starting a slice, verify:
- [ ] Feature has clear boundaries
- [ ] Dependencies are documented
- [ ] Existing tests exist or can be created
- [ ] Rollback plan is defined
- [ ] Success metrics are identified

## Standard steps per slice

For each slice, follow these steps strictly and in order:

1. **Analyze current implementation**
   - Locate relevant controllers, views, services, repositories in Presentation\Nop.Web and Libraries\Nop.*.
   - Identify any plugins (under Plugins\*) participating in this feature.
   - Document current routes, inputs, outputs, and key business rules in migration-log.md.

2. **Design .NET 8 boundary**
   - Define what part of the feature will be handled by a new .NET 8 component (service or application).
   - Specify contracts: URLs, request/response DTOs, error handling, authentication/authorization assumptions.
   - Keep contracts close to existing view models and DTOs to minimize change.

3. **Add .NET 8 project or endpoint**
   - Create or extend a .NET 8 project to implement the designed contracts.
   - Implement minimal logic required for this slice (can call existing data or be backed by its own data store, depending on your plan).

4. **Implement strangler adapter in nopCommerce 3.90**
   - Introduce an adapter layer (controller, service, or plugin) that:
     - Calls the .NET 8 endpoint.
     - Maps data between existing models and the new DTOs.
   - Avoid changing unrelated controllers, services, or plugins.

5. **Add or update tests**
   - Port or create tests for this slice that validate existing behavior using the new adapter and .NET 8 endpoint.
   - Prefer automated tests where possible; document any manual steps required.

6. **Verify slice**
   - Run build and tests.
   - Perform basic HTTP checks (e.g., storefront and/or admin URLs for this slice).
   - Compare critical behavior with pre-migration behavior where feasible.

7. **Flip traffic for slice**
   - Ensure the 3.90 instance uses the adapter for this slice by default.
   - Confirm that old code paths are no longer used for this slice (or are clearly marked as deprecated).

8. **Record status**
   - Update migration-log.md with:
     - Slice name.
     - Files touched.
     - Verification status.
     - Any known limitations or follow-up items.

## Testing Strategy

### Test Pyramid for Migration

#### 1. Unit Tests (70% of tests)
**Purpose**: Verify individual components in isolation

**For .NET 8 Components:**
- Test new services, repositories, and business logic
- Mock external dependencies (database, HTTP clients)
- Use xUnit, NUnit, or MSTest
- Aim for >80% code coverage on new code

**For Adapters:**
- Test mapping logic between old and new DTOs
- Verify error handling and fallback behavior
- Mock both .NET Framework services and .NET 8 HTTP clients

**Example Test Structure:**
```
Tests/
  Nop.Core8.Tests/           # .NET 8 component tests
    Services/
    Repositories/
  Nop.Adapters.Tests/        # Adapter layer tests
    Mappers/
    HttpClients/
```

#### 2. Integration Tests (20% of tests)
**Purpose**: Verify components work together correctly

**Database Integration:**
- Test EF Core DbContext against real SQL Server (or LocalDB/SQLite for CI)
- Use test containers (Testcontainers.DotNet) for isolated database instances
- Verify migrations and schema compatibility

**API Integration:**
- Test .NET 8 endpoints with WebApplicationFactory
- Verify request/response contracts
- Test authentication and authorization flows

**Adapter Integration:**
- Test full flow: .NET Framework → Adapter → .NET 8 → Database
- Use in-memory test server for .NET 8 side
- Verify data consistency

#### 3. Contract Tests (5% of tests)
**Purpose**: Ensure compatibility between .NET Framework and .NET 8 components

**Approach:**
- Use Pact or similar contract testing framework
- Define contracts for HTTP APIs between versions
- Verify request/response schemas match expectations
- Run on both consumer (.NET Framework) and provider (.NET 8) sides

**Key Contracts to Test:**
- DTO serialization/deserialization
- Error response formats
- Authentication token formats
- API versioning headers

#### 4. End-to-End Tests (5% of tests)
**Purpose**: Verify critical user journeys work across both versions

**Scope:**
- Smoke tests for each migrated slice
- Critical paths: registration → login → browse → add to cart → checkout
- Admin workflows: login → manage products → process orders

**Tools:**
- Selenium/Playwright for UI testing
- Postman/REST Client for API testing
- Keep tests focused on happy paths and critical errors

**Test Environments:**
- Staging environment with both .NET Framework and .NET 8 running
- Production-like data and configuration
- Monitoring and logging enabled

### Testing Per Slice

For each slice, follow this testing sequence:

#### Before Migration
1. **Baseline Tests**: Capture existing behavior
   - Run existing unit/integration tests
   - Record performance metrics (response times, throughput)
   - Document expected outputs for key scenarios

2. **Create Characterization Tests**: If tests don't exist
   - Write tests that describe current behavior
   - Focus on public interfaces and observable behavior
   - Don't fix bugs yet - just document current state

#### During Migration
3. **TDD for New Components**
   - Write tests for .NET 8 components first
   - Implement to make tests pass
   - Refactor with confidence

4. **Adapter Tests**
   - Test mapping logic thoroughly
   - Verify error handling (network failures, timeouts)
   - Test fallback to old code path if applicable

5. **Integration Tests**
   - Test new components against real database
   - Verify API contracts
   - Test authentication/session sharing

#### After Migration
6. **Regression Tests**
   - Run all existing tests
   - Verify no unintended side effects
   - Compare with baseline metrics

7. **Comparison Tests**
   - Run same requests against old and new implementations
   - Compare responses (may need to normalize timestamps, IDs)
   - Verify performance improvements

8. **Load Tests**
   - Use k6, JMeter, or Artillery
   - Compare performance: old vs new
   - Verify .NET 8 components handle expected load

### Test Data Strategy

**Approach:**
- Use shared test database with known seed data
- Create test data builders for complex entities
- Use realistic data volumes for performance tests
- Anonymize production data for staging tests

**Test Data Management:**
```
Tests/
  TestData/
    Seeds/              # SQL scripts for seed data
    Builders/           # Fluent builders for test entities
    Fixtures/           # Shared test fixtures
```

### Continuous Testing

**CI/CD Pipeline:**
1. **On Pull Request:**
   - Run unit tests (fast feedback)
   - Run integration tests for changed components
   - Run contract tests
   - Static analysis and code coverage

2. **On Merge to Main:**
   - Run full test suite
   - Run E2E smoke tests
   - Deploy to staging
   - Run load tests on staging

3. **Before Production Deploy:**
   - Run full E2E test suite on staging
   - Manual exploratory testing for critical paths
   - Performance comparison tests
   - Security scanning

### Test Metrics & Success Criteria

**Track per slice:**
- Test coverage: >80% for new code
- Test execution time: <5 minutes for unit tests, <15 minutes for integration
- Flaky test rate: <2%
- Performance comparison: .NET 8 should be ≥ old performance (ideally better)

**Quality Gates:**
- All tests must pass before merging
- No decrease in code coverage
- No new critical/high security vulnerabilities
- Performance within acceptable range

### Testing Tools & Frameworks

**Unit Testing:**
- xUnit or NUnit
- Moq or NSubstitute for mocking
- FluentAssertions for readable assertions
- AutoFixture for test data generation

**Integration Testing:**
- WebApplicationFactory (ASP.NET Core)
- Testcontainers for database/Redis
- Respawn for database cleanup between tests

**E2E Testing:**
- Playwright or Selenium
- SpecFlow for BDD scenarios (optional)

**Performance Testing:**
- BenchmarkDotNet for micro-benchmarks
- k6 or JMeter for load testing
- Application Insights or Prometheus for monitoring

**Contract Testing:**
- Pact.NET
- Or custom JSON schema validation

## Guardrails for agents

- Do not attempt to upgrade the entire solution to .NET 8 in one step.
- Do not remove or rewrite core nopCommerce abstractions (e.g., plugin interfaces, core services) without explicit human confirmation.
- Prefer introducing new code in separate projects or clearly separated folders over invasive changes to core libraries.
- Always consult migration-log.md before picking the next slice.
