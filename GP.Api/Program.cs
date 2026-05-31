using GP.API.Extensions;
using GP.Infrastructure.Hubs;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/api-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting up the API...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/api-log-.txt", rollingInterval: RollingInterval.Day));

    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
    builder.Services.AddSignalR();

    if (FirebaseApp.DefaultInstance == null)
    {
        var credentialsPath = builder.Configuration["Firebase:CredentialsPath"];
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new InvalidOperationException("Firebase credentials path is not configured.");
        }

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);

        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.GetApplicationDefault()
        });
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SignalRCorsPolicy", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:4200",
                    "http://domain.com",
                    "https://domain.com"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Transport Booking API",
                Version = "v1",
                Description = "API for transportation booking system"
            };

            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT token (without 'Bearer' prefix)"
                }
            };

            document.SecurityRequirements = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    }] = Array.Empty<string>()
                }
            };

            return Task.CompletedTask;
        });
    });

    var app = builder.Build();

    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Transport Booking API")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.UseRateLimiter();
    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors("SignalRCorsPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not "HostAbortedException")
{
    Log.Fatal(ex, "The API failed to start correctly.");
}
finally
{
    Log.CloseAndFlush();
}