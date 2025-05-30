﻿using ANF.Core;
using ANF.Core.Commons;
using ANF.Core.Services;
using ANF.Infrastructure;
using ANF.Service;
using ANF.Service.Backgrounds;
using ANF.Service.RabbitMQ;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ANF.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Register(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers().AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opt.JsonSerializerOptions.WriteIndented = true;
                opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
            services.AddEndpointsApiExplorer();
            // Options pattern: Must add this line to run properly when injecting the configuration class
            // By default, the configuration binder uses a case-insensitive matching
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
            services.Configure<PayOSSettings>(configuration.GetSection("PayOS"));
            services.Configure<BankLookupSettings>(configuration.GetSection("BankLookup"));
            services.Configure<IpApiSettings>(configuration.GetSection("IpApi"));
            services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));

            // Override the default configuration of 400 HttpStatusCode for all controllers
            services.Configure<ApiBehaviorOptions>(opt =>
            {
                opt.SuppressModelStateInvalidFilter = true;
            });

            var connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
            var jwtConfig = configuration.GetRequiredSection("Jwt");

            services.AddHttpContextAccessor();
            services.ConfigureSwagger();
            services.ConfigureCors(configuration);
            services.ConfigureAuthentication(jwtConfig);
            services.ConfigureDatabase(connectionString);

            services.AddAutoMapper(typeof(MappingProfileExtension));
            services.AddApiVersioning();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddApplicationService();
            services.AddMemoryCache();
            services.AddHttpClient();
            services.AddSignalR();
            services.AddSingleton<RabbitMQPublisher>();

            #region Hosted services
            services.AddHostedService<RabbitMQConsumer>();
            services.AddHostedService<SampleIdDetectionService>();
            services.AddHostedService<PostbackValidationService>();
            services.AddHostedService<CampaignBackgroundService>();
            services.AddHostedService<StatisticBackgroundService>();
            services.AddHostedService<AdminStatsBackgroundService>();
            services.AddHostedService<AdvertiserCampaignStatsBackgroundService>();
            #endregion

            services.Configure<ForwardedHeadersOptions>(o =>
            {
                // Cloudflare IPv4 ranges (current list from Cloudflare)
                var cloudflareNets = new[]
                {
                    IPNetwork.Parse("173.245.48.0/20"),
                    IPNetwork.Parse("103.21.244.0/22"),
                    IPNetwork.Parse("103.22.200.0/22"),
                    IPNetwork.Parse("103.31.4.0/22"),
                    IPNetwork.Parse("141.101.64.0/18"),
                    IPNetwork.Parse("108.162.192.0/18"),
                    IPNetwork.Parse("190.93.240.0/20"),
                    IPNetwork.Parse("188.114.96.0/20"),
                    IPNetwork.Parse("197.234.240.0/22"),
                    IPNetwork.Parse("198.41.128.0/17"),
                    IPNetwork.Parse("162.158.0.0/15"),
                    IPNetwork.Parse("104.16.0.0/13"),
                    IPNetwork.Parse("104.24.0.0/14"),
                    IPNetwork.Parse("172.64.0.0/13"),
                    IPNetwork.Parse("131.0.72.0/22"),
                };

                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                foreach (var item in cloudflareNets)
                {
                    o.KnownNetworks.Add(item);
                }
            });

            return services;
        }

        /// <summary>
        /// Configures Cross-Origin Resource Sharing (CORS) for the application.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the CORS policy to.</param>
        /// <param name="configuration">The IConfiguration to read CORS settings from.</param>
        /// <returns>The IServiceCollection with the CORS policy added.</returns>
        private static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration
                .GetSection("CorsSettings:AllowedOrigins")
                .Get<string[]>() ?? [];

            services.AddCors(opt =>
            {
                opt.AddPolicy("ANF", builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });
            return services;
        }

        /// <summary>
        /// Configures Swagger for the application.
        /// </summary>
        /// <param name="services">The IServiceCollection to add Swagger to.</param>
        /// <returns>The IServiceCollection with Swagger configured.</returns>
        private static IServiceCollection ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "SEP490.AffiliateNetwork",
                    Description = "API for Affiliate Network Platform",
                    //TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Support",
                        Email = "support@example.com",
                        Url = new Uri("https://example.com/contact")
                    },
                    //License = new OpenApiLicense
                    //{
                    //    Name = "Use under LICX",
                    //    Url = new Uri("https://example.com/license")
                    //}
                });

                // using System.Reflection;
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                // Define JWT Bearer Token support
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token in the format: `Bearer {token}`"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>() // No specific scopes required
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// Configures the database context for the application.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the database context to.</param>
        /// <param name="connection">The connection string for the database.</param>
        /// <returns>The IServiceCollection with the database context configured.</returns>
        private static IServiceCollection ConfigureDatabase(this IServiceCollection services, string connection)
        {
            services.AddDbContext<ApplicationDbContext>(opt =>
            {
                opt.UseSqlServer(connection, options =>
                {
                    options.CommandTimeout(120);
                });
            });
            return services;
        }

        /// <summary>
        /// Adds API versioning to the application.
        /// </summary>
        /// <param name="services">The IServiceCollection to add API versioning to.</param>
        /// <returns>The IServiceCollection with API versioning added.</returns>
        private static IServiceCollection AddApiVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = new ApiVersion(1);
                opt.ReportApiVersions = true;
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version"));
            })
            .AddApiExplorer(opt =>
            {
                opt.GroupNameFormat = "'v'V";
                opt.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        /// <summary>
        /// Adds application-specific services to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to.</param>
        /// <returns>The IServiceCollection with the application-specific services added.</returns>
        private static IServiceCollection AddApplicationService(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<ICampaignService, CampaignService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPublisherService, PublisherService>();
            services.AddScoped<IAdvertiserService, AdvertiserService>();
            services.AddScoped<ITrackingService, TrackingService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IOfferService, OfferService>();
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped(typeof(TokenService));
            services.AddScoped<IUserClaimsService, UserClaimsService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddTransient(typeof(HttpClient));
            services.AddScoped<IPolicyService, PolicyService>();
            services.AddScoped<IPostbackService, PostbackService>();
            services.AddScoped<IStatisticService, StatisticService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IPublisherStatsService, PublisherStatsService>();
            services.AddScoped<IFraudDetectionService, FraudDetectionService>();

            #region Background services
            services.AddSingleton<SampleIdDetectionService>();
            services.AddSingleton<PostbackValidationService>();
            services.AddSingleton<CampaignBackgroundService>();
            services.AddSingleton<AdminStatsBackgroundService>();
            services.AddSingleton<PublisherStatsBackgroundService>();
            services.AddSingleton<AdvertiserCampaignStatsBackgroundService>();
            #endregion
            return services;
        }

        private static IServiceCollection ConfigureAuthentication(this IServiceCollection services,
            IConfigurationSection jwtSection)
        {
            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
            {
                var configKey = jwtSection["Key"] ?? string.Empty;
                var key = Encoding.UTF8.GetBytes(configKey);

                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                };

                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notiHub"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });  // NOTE: Can add more authentication schema with configurations
            return services;
        }
    }
}
