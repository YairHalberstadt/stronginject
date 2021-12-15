using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StrongInject.Extensions.DependencyInjection.AspNetCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Extensions.DependencyInjection.Tests
{
    [ApiController]
    [Route("[controller]")]
    public class Controller1 : ControllerBase
    {
        [HttpGet] public string Get() => "Controller 1!";
    }

    [ApiController]
    [Route("[controller]")]
    public class Controller2 : ControllerBase
    {
        [HttpGet] public string Get() => "Controller 2!";
    }

    public class MvcBuilderExtensionTests
    {
        public class Startup
        {
            public Startup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            public IConfiguration Configuration { get; }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddControllers().ResolveControllersThroughServiceProvider();
            }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            }
        }

        [Fact]
        public async Task ResolveControllersThroughServiceProvider_SuceedsIfRequestedControllerIsRegistered1()
        {
            var hostBuilder = new WebHostBuilder()
                .ConfigureServices(x => x.AddTransient<Controller1>())
                .UseStartup<Startup>();
            var server = new TestServer(hostBuilder);
            Assert.Equal("Controller 1!", await server.CreateClient().GetStringAsync("/controller1"));
        }

        [Fact]
        public async Task ResolveControllersThroughServiceProvider_SuceedsIfRequestedControllerIsRegistered2()
        {
            var hostBuilder = new WebHostBuilder()
                .ConfigureServices(x => x.AddTransient<Controller2>())
                .UseStartup<Startup>();
            var server = new TestServer(hostBuilder);
            Assert.Equal("Controller 2!", await server.CreateClient().GetStringAsync("/controller2"));
        }

        [Fact]
        public async Task ResolveControllersThroughServiceProvider_ErrorIfRequestedControllerIsNotRegistered()
        {
            var hostBuilder = new WebHostBuilder()
                .ConfigureServices(x => x.AddTransient<Controller1>())
                .UseStartup<Startup>();
            var server = new TestServer(hostBuilder);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateClient().GetStringAsync("/controller2"));
            Assert.Equal("No service for type 'StrongInject.Extensions.DependencyInjection.Tests.Controller2' has been registered.", ex.Message);
        }
    }
}
