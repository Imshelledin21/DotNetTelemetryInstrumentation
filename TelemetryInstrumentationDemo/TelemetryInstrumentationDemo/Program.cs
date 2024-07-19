using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Oracle.ManagedDataAccess.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "dice-roll-application";

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Instrument OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))

    //OpenTelemetry Metrics Auto-Instrumentation
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()  // Do not use in production. For local testing only. 
        .AddPrometheusExporter()) // Exports Metrics in Prometheus format.

    // OpenTelemetry Traces Auto-Instrumentation
    .WithTracing(builder =>
    {
        builder.AddAspNetCoreInstrumentation(opts =>
        {
            opts.EnrichWithHttpRequest = (activity, httprequestmessage) =>
            {
                activity.DisplayName = httprequestmessage.PathBase;
            };
            
        })
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("GrafanaStackTelemetry"))
        .AddSource("GrafanaStack.Traces")
        .AddHttpClientInstrumentation(opts => // Trace calls to external HTTP resources. See more: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Instrumentation.Http/README.md
        {
            // Note: Only called on .NET & .NET Core runtimes.
            opts.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
            {
                activity.SetTag("requestVersion", httpRequestMessage.Version);
                activity.DisplayName = "Testing this functionality";
            };
            // Note: Only called on .NET & .NET Core runtimes.
            opts.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
            {
                activity.SetTag("responseVersion", httpResponseMessage.Version);
            };
            // Note: Called for all runtimes
            opts.EnrichWithException = (activity, exception) =>
            {
                activity.SetTag("stackTrace", exception.StackTrace);
            };
        })
        // Trace calls to Oracle Databases
        .AddOracleDataProviderInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true; // Include DB Query Statement in span attributes
            opts.SetDbStatementForStoredProcedure = true; // Include Called Stored Procedure in span attributes
        })

        // Trace calls to MS SQL Databases
        .AddSqlClientInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true; // enable writing sql query to span attribute
            opts.SetDbStatementForStoredProcedure = true; // enable writing stored procedure name to span attribute
        }) 

        .AddConsoleExporter()  // Write Trace information to console (Do not use in Production)
        .AddOtlpExporter(opts => //OTLP Exporter setup
        {
            opts.Endpoint = new Uri("http://localhost:4317");
        });
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Register and Configure Prometheus Scraping Middleware
app.UseOpenTelemetryPrometheusScrapingEndpoint(); // Default path '/metrics'

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
