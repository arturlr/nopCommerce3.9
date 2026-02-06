using System;
using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Services.Catalog;
using Nop.Services.Customers;

namespace Nop.Services.Infrastructure
{
    /// <summary>
    /// Dependency registrar for Services layer
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            // Register HttpCatalogAdapter as decorator for ICategoryService when feature flag is enabled
            var useDotNet8Api = Environment.GetEnvironmentVariable("USE_DOTNET8_API") == "true";
            if (useDotNet8Api)
            {
                builder.RegisterDecorator<HttpCatalogAdapter, ICategoryService>();
                builder.RegisterDecorator<HttpCustomerAdapter, ICustomerRegistrationService>();
                builder.RegisterDecorator<HttpCustomerProfileAdapter, ICustomerService>();
            }
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 1; }
        }
    }
}