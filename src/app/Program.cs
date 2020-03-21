namespace hmac
{
    using System.Threading.Tasks;
    using app;
    using Blazored.LocalStorage;
    using Core;
    using Microsoft.AspNetCore.Blazor.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services
                .AddScoped<HMACProcessor, HMACProcessor>()
                .AddScoped<IToastController, ToastController>()
                .AddBlazoredLocalStorage();
            builder.RootComponents.Add<App>("app");

            await builder.Build().RunAsync();
        }
    }
}
