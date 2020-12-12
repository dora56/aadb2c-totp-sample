using Functions;
using Functions.Models;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Functions
{
    public class Startup: FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddOptions<AppSettings>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("AppSettings").Bind(settings);
                });
        }
    }
}
