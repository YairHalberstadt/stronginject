#define DontAutoRegisterControllers

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StrongInject.Extensions.DependencyInjection;
using StrongInject.Samples.AspNetCore.Controllers;

namespace StrongInject.Samples.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if DontAutoRegisterControllers
            services.AddControllers().ResolveControllersThroughServiceProvider(); // Tells ASP.NET to resolve controllers through the ServiceProvider, rather than calling their constructors directly.
            services.AddTransientServiceUsingContainer<Container, WeatherForecastController>(); // register the controller with the ServiceProvider.
            services.AddTransientServiceUsingContainer<Container, UsersController>();
#else
            services.AddControllers().AddControllersAsServices(); // Tells ASP.NET to resolve controllers through the ServiceProvider, rather than calling their constructors directly, and then auto registers all controllers with the service provider.
            services.ReplaceWithTransientServiceUsingContainer<Container, WeatherForecastController>(); // register the controller with the ServiceProvider, and remove the existing registration that was added automatically by AddControllersAsServices().
            services.ReplaceWithTransientServiceUsingContainer<Container, UsersController>();
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
