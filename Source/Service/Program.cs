using Sentry.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Service.Application;
using Service.Common.Logging;
using Monitor = Service.Common.Monitoring.Monitor;

// Bootstrap the application
var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Build service container
builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    //options.OperationFilter<FileResultContentTypeOperationFilter>();
});

// Configure host
builder.Host
    .UseSerilog((context, services, config) => config
        .ReadFrom.Services(services)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            theme: AnsiConsoleTheme.Code,
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
        .WriteTo.Sentry());

builder.WebHost
    .UseSentry(o => o
        .AddSentryOptions(options =>
        {
            options.DefaultTags.Add("Service", "Service-Name");
            options.SendDefaultPii = true;
            options.AttachStacktrace = true;
            options.MinimumBreadcrumbLevel = LogLevel.Debug;
            options.MinimumEventLevel = LogLevel.Warning;
        }));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(o =>
{
    o.EnrichDiagnosticContext = LogHelper.EnrichFromRequest;
    o.MessageTemplate += " via {EndpointName}";
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Initialize application with monitor
await Monitor.Run(args, _ => app.RunAsync(), ex =>
{
    if (ex is null)
    {
        Log.Information("Shutting down...");
    }
    else
    {
        Log.Fatal(ex, "Exception thrown: {Exception}", ex.Message);
    }

    Log.CloseAndFlush();

    return ValueTask.CompletedTask;
});
