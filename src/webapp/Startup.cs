using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.ResourceDetectors.Container;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace integration_test_app
{
    public class Startup
    {
        private static readonly Meter MyMeter = new Meter("MyCompany.MyProduct.MyLibrary", "1.0");
        private static readonly Counter<long> MyFruitCounter = MyMeter.CreateCounter<long>("MyFruitCounter");

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("aws-otel-integ-test").AddTelemetrySdk())
                .AddXRayTraceId()
                .AddAWSInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
                })
                .Build();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("aws-otel-integ-test").AddTelemetrySdk().AddDetector(new ContainerResourceDetector()))
                .AddMeter("MyCompany.MyProduct.MyLibrary")
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddConsoleExporter((exporterOptions, metricReaderOptions) =>
                {
                    metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
                })
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
                })
                .Build();

            MyFruitCounter.Add(1, KeyValuePair.Create<string, object>("name", "apple"), KeyValuePair.Create<string, object>("color", "red"));
            MyFruitCounter.Add(2, KeyValuePair.Create<string, object>("name", "lemon"), KeyValuePair.Create<string, object>("color", "yellow"));
            MyFruitCounter.Add(1, KeyValuePair.Create<string, object>("name", "lemon"), KeyValuePair.Create<string, object>("color", "yellow"));
            MyFruitCounter.Add(2, KeyValuePair.Create<string, object>("name", "apple"), KeyValuePair.Create<string, object>("color", "green"));
            MyFruitCounter.Add(5, KeyValuePair.Create<string, object>("name", "apple"), KeyValuePair.Create<string, object>("color", "red"));
            MyFruitCounter.Add(4, KeyValuePair.Create<string, object>("name", "lemon"), KeyValuePair.Create<string, object>("color", "yellow"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
