using Basket.API.Extensions;
using Basket.API.Models;
using Basket.API.Services;
using Basket.API.Services.Interfaces;
using LogCorner.EduSync.Speech.Telemetry.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QueueFactory;
using QueueFactory.Models;
using Serilog;

namespace Basket.API;

public class Program
{
    public static ConfigurationManager Configuration { get; private set; }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Configuration = builder.Configuration;

        builder.Services.AddCORSPolicy(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        builder.UseSerilog(Configuration);
        builder.Services.AddRouting(options => options.LowercaseUrls = true);

        builder.Services.AddHttpClient<ICatalogService, CatalogService>();
        builder.Services.AddSingleton(sp => RabbitMQFactory.CreateBus(BusType.LocalHost));

        //Action<ResourceBuilder> appResourceBuilder = AddTelemetrySdk();

        builder.AddOpenTelemetry(Configuration);
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors(Policies.CORS_MAIN);
        app.UseAuthorization();
        app.UseSerilogRequestLogging();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            AllowCachingResponses = false,
            ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
        });
        app.MapControllers();

        app.Run();
    }
}