using System;
using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;

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
                builder.RegisterDecorator<HttpShoppingCartAdapter, IShoppingCartService>();
                builder.RegisterDecorator<HttpPaymentAdapter, IPaymentService>();
                builder.RegisterDecorator<HttpShippingAdapter, IShippingService>();
                builder.RegisterDecorator<HttpWidgetAdapter, IWidgetService>();
                
                // Register HttpOrderAdapter for order management
                builder.RegisterType<HttpOrderAdapter>().AsSelf().InstancePerLifetimeScope();
                
                // Register HttpAdminOrderAdapter for admin order management
                builder.RegisterType<HttpAdminOrderAdapter>().AsSelf().InstancePerLifetimeScope();
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