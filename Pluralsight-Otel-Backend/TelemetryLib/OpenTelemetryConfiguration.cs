using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace LogCorner.EduSync.Speech.Telemetry.Configuration
{
    public static class OpenTelemetryConfiguration
    {
        public static void UseSerilog(this WebApplicationBuilder builder, IConfiguration configuration)
        {
            builder.Host.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = $"{configuration.GetValue<string>("Otlp:Endpoint")}/v1/logs";
                    options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = configuration.GetValue<string>("Otlp:ServiceName")
                    };
                }));
        }

        public static void AddOpenTelemetry(this WebApplicationBuilder builder, IConfiguration configuration)
        {
            Action<ResourceBuilder> appResourceBuilder =
                resource => resource
                    .AddTelemetrySdk()
                    .AddService(configuration.GetValue<string>("Otlp:ServiceName"));

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(appResourceBuilder)
                .WithTracing(builder => builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("APITracing")
                .AddConsoleExporter()
                    .AddOtlpExporter(options => options.Endpoint = new Uri(configuration.GetValue<string>("Otlp:Endpoint")))
                    .AddZipkinExporter(b =>
                    {
                        var zipkinHostName = configuration["Zipkin:Hostname"];
                        var zipkinPort = configuration["Zipkin:PortNumber"];

                        var endpoint = new Uri($"http://{zipkinHostName}:{zipkinPort}/api/v2/spans");
                        b.Endpoint = endpoint;
                    })
                 )
                .WithMetrics(builder => builder
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddOtlpExporter(options => options.Endpoint = new Uri(configuration.GetValue<string>("Otlp:Endpoint")))
                    );
        }
    }
}