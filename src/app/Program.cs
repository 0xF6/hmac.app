namespace hmac
{
    using System.Threading.Tasks;
    using app;
    using hmac.Core;
    using Microsoft.AspNetCore.Blazor.Hosting;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(HMACProcessor), typeof(HMACProcessor), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped));
            builder.RootComponents.Add<App>("app");

            await builder.Build().RunAsync();
        }
    }
}
