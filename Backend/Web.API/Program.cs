using Serilog;
using Web.API;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog before building the app
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  // Read configuration from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Month)
    .CreateLogger();

// Log starting message
Log.Information("Starting server.");

// Configure Serilog for the application host
builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(); // Include console logging
});

// Add services to the container
builder.Services.AddInfrastructure(builder.Configuration);

// Build the application
var app = builder.Build();


// Configure the HTTP request pipeline
app.UseWebApiPipeline();

// Run the application
app.Run();
