using GP.API.Extensions;
using GP.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddSignalR();

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

// Configure OpenAPI/Swagger with JWT support
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

//builder.Services.AddHttpsRedirection(options =>
//{
//
//    options.HttpsPort = 44399;
//});

var app = builder.Build();

// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
app.MapOpenApi();

// Use Scalar UI
app.MapScalarApiReference(options =>
{
    options.WithTitle("Transport Booking API")
           .WithTheme(ScalarTheme.Purple)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// Swagger UI
// app.UseSwaggerUI(c =>
//{
//     c.SwaggerEndpoint("/openapi/v1.json", "Transport Booking API V1");
// });
//}

app.UseRateLimiter();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("SignalRCorsPolicy"); //Todo: Change to "Production" for production
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.Run();