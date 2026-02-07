# Technology Stack

## Framework & Language
- **Language**: C# (.NET Framework 4.5.1)
- **Web Framework**: ASP.NET MVC 5.2.3
- **ORM**: Entity Framework 6.x (Code First approach)
- **Database**: MS SQL Server 2008 or higher (also supports SQL Server Compact Edition)

## Key Dependencies
- **Autofac 4.4.0**: Dependency injection container
- **AutoMapper 5.2.0**: Object-to-object mapping
- **Newtonsoft.Json 9.0.1**: JSON serialization
- **StackExchange.Redis 1.2.1**: Redis caching support
- **RedLock.net 1.7.4**: Distributed locking for Redis

## Build System
- **Build Tool**: MSBuild (Visual Studio 2015+)
- **Solution File**: `src/NopCommerce.sln`
- **Package Manager**: NuGet

## Common Commands

### Building
```bash
# Build entire solution (from src directory)
msbuild NopCommerce.sln /p:Configuration=Release

# Build in Debug mode
msbuild NopCommerce.sln /p:Configuration=Debug
```

### Running Tests
```bash
# Run all tests (requires test runner like NUnit or MSTest)
# Test projects are in Tests/ folder
```

### Package Restore
```bash
# Restore NuGet packages
nuget restore NopCommerce.sln
```

## Development Environment
- **IDE**: Visual Studio 2015 or later recommended
- **Target Framework**: .NET Framework 4.5.1
- **Platform**: Any CPU

## Architecture Patterns
- **Repository Pattern**: Data access abstraction via `IRepository<T>`
- **Dependency Injection**: Constructor injection using Autofac
- **Plugin Architecture**: Dynamic plugin loading via MEF-style discovery
- **Domain-Driven Design**: Rich domain models in `Nop.Core.Domain`
