namespace GP.API.Extensions;

using FluentValidation;
using FluentValidation.AspNetCore;
using GP.API.Filters;
using GP.API.Middleware;
using GP.Application.Common;
using GP.Application.DTOs.Bookings;
using GP.Application.Events;
using GP.Application.Interfaces;
using GP.Application.Services;
using GP.Application.Settings;
using GP.Application.Validators;
using GP.Infrastructure.Data;
using GP.Infrastructure.Identity;
using GP.Infrastructure.Interfaces;
using GP.Infrastructure.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Controllers
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });
        services.AddEndpointsApiExplorer();
        services.AddMemoryCache();

        // Database
        services.AddDatabaseServices(configuration);

        // Identity
        services.AddIdentityServices();

        // Authentication & Authorization
        services.AddAuthenticationServices(configuration);

        services.AddValidationServices();

        // Application Services
        services.AddBusinessServices(configuration);

        // CORS 
        services.AddCorsServices();

        services.AddExceptionHandling();

        return services;
    }
    //FluentValidation
    private static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });
        //Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.PermitLimit = 10;
            });
        });

        return services;
    }

    public static IServiceCollection AddExceptionHandling(
        this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }


    private static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()
            )
        );

        return services;
    }



    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;

            // User settings
            options.User.RequireUniqueEmail = true;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            //Todo: Email confirmation 
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Token lifespan settings
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(24);
        });

        return services;
    }

    private static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        if (jwtSettings == null)
        {
            throw new InvalidOperationException("JWT Settings are not configured properly");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false; // Set to true in production
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };

            //logging/debugging
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequireAdminRole, policy => policy.RequireRole(Roles.Admin));
        });

        return services;
    }

    private static IServiceCollection AddBusinessServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
     {
         cfg.RegisterServicesFromAssembly(typeof(BookingCompletedEvent).Assembly);
     });

        // Auth Service
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthenticationService>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IAdminUserService, AdminUserService>();

        // Register Data Seeders
        services.AddScoped<GP.Infrastructure.Services.MasterStationSeeder>();
        services.AddScoped<GP.Infrastructure.Services.GoBusTripSeeder>();
        services.AddScoped<GP.Infrastructure.Services.HorusTripSeeder>();
        services.AddScoped<GP.Infrastructure.Services.BlueBusTripSeeder>();
        services.AddScoped<GP.Infrastructure.Services.EnrTripSeeder>();

        // User profile & files 
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // Trip Occurrence Generator
        services.AddScoped<ITripOccurrenceService, TripOccurrenceService>();

        // Station Service
        services.AddScoped<IStationService, StationService>();

        // Occurrence Seat Map Service
        services.AddScoped<IOccurrenceSeatService, OccurrenceSeatService>();

        // Search Service
        services.AddScoped<ISearchService, SearchService>();

        // Booking Service
        services.AddScoped<IBookingService, BookingService>();

        // Wallet Service
        services.AddScoped<IWalletService, WalletService>();

        // Loyalty Service
        services.AddScoped<ILoyaltyService, LoyaltyService>();

        // Marketplace Service
        services.AddScoped<IMarketplaceService, MarketplaceService>();

        // Notifications Service
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    private static IServiceCollection AddCorsServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // policy for production
            options.AddPolicy("Production", policy =>
            {
                policy.WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }
}