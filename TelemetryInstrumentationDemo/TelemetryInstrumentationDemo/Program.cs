using Prometheus;
using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Documentation: https://opentelemetry.io/docs/instrumentation/net/getting-started/
// This is a very bare bones instrumentation, but with this alone, traces will be capture and exported to the console and a configured OLTP collector.
// By default, it will send the spans to localhost:4317, this can be changed based upon the configuration of your trace collector, such as the Grafana Agent. 
// This will also identify http/grpc calls, as well as calls to SQL server, generating spans for those calls automatically. 
// If the calling application is also instrumented for tracing, the trace will continue through that service, and any calls it makes

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(DiagnosticsConfig.ActivitySource.Name)
            .ConfigureResource(resource => resource
                .AddService(DiagnosticsConfig.ServiceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configuration for enabling a metrics endpoint and displaying base metrics. 
// If generating custom metrics, the prometheus asp.net package will add exemplars to your metrics, allowing you to view traces from your metrics. 
app.UseHttpMetrics();
app.UseMetricServer();



app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


public static class DiagnosticsConfig
{
    public const string ServiceName = "MyService";
    public static ActivitySource ActivitySource = new ActivitySource(ServiceName);
}