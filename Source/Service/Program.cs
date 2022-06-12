using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Service.Application;
using Service.Common.Logging;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
