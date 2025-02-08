using System.Net.Mime;
using System.Text.Json.Serialization;
using ChannelSample.AppHost;
using ChannelSample.AppHost.Channels;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers(options =>
	{
		options.Filters.Add(new ProducesAttribute(MediaTypeNames.Application.Json));
		options.Filters.Add(new ConsumesAttribute(MediaTypeNames.Application.Json));
	})
	.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddOptions<ChannelSettingsOptions>()
	.Configure((ChannelSettingsOptions options) => builder.Configuration.GetSection("ChannelSettings").Bind(options));

builder.Services.AddSingleton<IMultipleDispatcher, MultipleDispatcher>();

builder.Services.AddSingleton<SingleChannel>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOpenTelemetry()
	.ConfigureResource(resource => resource
	.AddService(builder.Configuration["SERVICE_NAME"]!))
	.UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(builder.Configuration["OTLP_ENDPOINT_URL"]!))
	.WithMetrics(metrics => metrics
		//.SetResourceBuilder(ResourceBuilder.CreateDefault()
		//.AddEnvironmentVariableDetector())
		.AddMeter("MassTransitRabbitMQSample.")
		.AddPrometheusExporter()
		//.AddConsoleExporter()
		.AddRuntimeInstrumentation()
		.AddAspNetCoreInstrumentation())
	.WithTracing(tracing => tracing
		//.SetResourceBuilder(ResourceBuilder.CreateDefault()
		//.AddEnvironmentVariableDetector())
		.AddHttpClientInstrumentation()
		.AddGrpcClientInstrumentation()
		.AddGrpcCoreInstrumentation()
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddAspNetCoreInstrumentation(options => options.Filter = (httpContext) =>
				!httpContext.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) &&
				!httpContext.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) &&
				!httpContext.Request.Path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase) &&
				!httpContext.Request.Path.Value!.Equals("/api/events/raw", StringComparison.OrdinalIgnoreCase) &&
				!httpContext.Request.Path.Value!.EndsWith(".js", StringComparison.OrdinalIgnoreCase) &&
				!httpContext.Request.Path.StartsWithSegments("/_vs", StringComparison.OrdinalIgnoreCase)))
	.WithLogging();

builder.Services
	.AddHealthChecks()
	.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	_ = app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
	var channel = scope.ServiceProvider.GetRequiredService<SingleChannel>();
	using var cts = new CancellationTokenSource();
	await channel.PrepareAsync(cts.Token).ConfigureAwait(false);
}

app.MapHealthChecks("/live", new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/healthz", new HealthCheckOptions
{
	Predicate = _ => true
});

app.Run();