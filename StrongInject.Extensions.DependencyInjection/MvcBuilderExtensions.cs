using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongInject.Extensions.DependencyInjection
{
    public static class MvcBuilderExtensions
    {
        /// <summary>
        /// Use the ServiceProvider to resolve controllers. Will error if the controller isn't explicitly registered
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder ResolveControllersThroughServiceProvider(this IMvcBuilder builder)
        {
            builder.PartManager.PopulateFeature(new ControllerFeature());
            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
            return builder;
        }
    }
}
