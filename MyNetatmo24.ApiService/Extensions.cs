using JasperFx.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;

namespace MyNetatmo24.ApiService;

public static class Extensions
{
    private static HeaderPolicyCollection? s_headerApiPolicy;

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddAuthentication()
        {
            // Add authentication and authorization using Auth0
            builder.Services.AddAuth0ApiAuthentication(options =>
            {
                options.Domain = builder.Configuration["Auth0:Domain"];
                options.JwtBearerOptions = new JwtBearerOptions
                {
                    Audience = builder.Configuration["Auth0:Audience"],
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier, RoleClaimType = "permissions"
                    }
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Constants.Policies.ReadWeather, b =>
                {
                    b.RequireAuthenticatedUser()
                        .RequireRole("read:weatherdata");
                });
            });

            return builder;
        }

        public WebApplicationBuilder AddErrorHandling()
        {
            builder.Services.AddExceptionHandler<ExceptionHandler>();
            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                };
            });
            return builder;
        }

        public WebApplicationBuilder AddCaching()
        {
            builder.AddRedisDistributedCache(connectionName: "cache");
            builder.Services.AddFusionCache()
                .WithOptions(options =>
                {
                    options.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2);
                })
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(1),

                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(2),
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(30),

                    EagerRefreshThreshold = 0.9f,

                    FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
                    FactoryHardTimeout = TimeSpan.FromMilliseconds(1500),

                    DistributedCacheSoftTimeout = TimeSpan.FromSeconds(1),
                    DistributedCacheHardTimeout = TimeSpan.FromSeconds(2),
                    AllowBackgroundDistributedCacheOperations = true,

                    JitterMaxDuration = TimeSpan.FromSeconds(2)
                });

            builder.Services
                .AddFusionCacheSystemTextJsonSerializer()
                .AddFusionCacheStackExchangeRedisBackplane(options =>
                {
                    options.Configuration = builder.Configuration.GetConnectionString("cache");
                })
                .AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddFusionCacheInstrumentation())
                .WithMetrics(metrics => metrics.AddFusionCacheInstrumentation());

            return builder;
        }

        public WebApplicationBuilder AddWolverine()
        {
            builder.Services.AddResourceSetupOnStartup();
            builder.Host.UseWolverine(options =>
            {
                // Required to generate the OpenAPI document, otherwise this exception is thrown
                if (Environment.GetCommandLineArgs()
                    .Any(e => e.Contains("GetDocument.Insider", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var connectionString = builder.Configuration.GetConnectionString(Constants.DatabaseName) ??
                                       throw new InvalidOperationException(
                                           $"Connection string '{Constants.DatabaseName}' not found.");
                options.PersistMessagesWithPostgresql(connectionString, "wolverine");
                options.UseEntityFrameworkCoreTransactions();
                options.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
                options.Durability.MessageIdentity = MessageIdentity.IdAndDestination;
                options.Policies.UseDurableLocalQueues();
                options.Policies.AutoApplyTransactions();
            });

            return builder;
        }

        public WebApplicationBuilder AddFastEndpointsWithOpenApi()
        {
            builder.Services
                .AddFastEndpoints(options => options.AddEndpointsAssemblies())
                .SwaggerDocument(options =>
                {
                    options.RemoveEmptyRequestSchema = true;
                    options.EnableJWTBearerAuth = false;
                    options.DocumentSettings = settings =>
                    {
                        settings.Title = "My Netatmo 24 API";
                        settings.Description = "An API to access weather data from Netatmo devices.";
                        settings.MarkNonNullablePropsAsRequired();
                        settings.AddAuth("Auth0", new OpenApiSecurityScheme
                        {
                            Type = OpenApiSecuritySchemeType.OAuth2,
                            BearerFormat = "JWT",
                            Scheme = "Bearer",
                            In = OpenApiSecurityApiKeyLocation.Header,
                            Flows = new OpenApiOAuthFlows
                            {
                                AuthorizationCode = new OpenApiOAuthFlow
                                {
                                    // AuthorizationUrl = $"https://{builder.Configuration["Auth0:Domain"]}/authorize?audience={builder.Configuration["Auth0:Audience"]}",
                                    AuthorizationUrl = $"https://{builder.Configuration["Auth0:Domain"]}/authorize",
                                    TokenUrl = $"https://{builder.Configuration["Auth0:Domain"]}/oauth/token",
                                    Scopes = new Dictionary<string, string>
                                    {
                                        { "openid", "OpenID Connect scope" },
                                        { "profile", "Access to your profile information" },
                                        { "email", "Access to your email address" },
                                        { "offline_access", "Access to refresh tokens" },
                                        { "read:weatherdata", "Read access to weather data" }
                                    }
                                }
                            }
                        });
                    };
                });

            return builder;
        }

        public WebApplicationBuilder AddSecurity()
        {
            builder.Services.AddSecurityHeaderPolicies()
                .SetPolicySelector(_ => GetApiHeaderPolicyCollection(builder.Environment.IsDevelopment()));

            return builder;
        }
    }

    internal static HeaderPolicyCollection GetApiHeaderPolicyCollection(bool isDevelopment)
    {
        if (s_headerApiPolicy is not null)
        {
            return s_headerApiPolicy;
        }

        s_headerApiPolicy = new HeaderPolicyCollection()
            .AddFrameOptionsDeny()
            .AddContentTypeOptionsNoSniff()
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .AddCrossOriginOpenerPolicy(builder => builder.SameOrigin())
            .AddCrossOriginEmbedderPolicy(builder => builder.RequireCorp())
            .AddCrossOriginResourcePolicy(builder => builder.SameOrigin())
            .RemoveServerHeader()
            .AddPermissionsPolicyWithDefaultSecureDirectives();

        s_headerApiPolicy.AddContentSecurityPolicy(builder =>
        {
            builder.AddObjectSrc().None();
            builder.AddBlockAllMixedContent();
            builder.AddImgSrc().None();
            builder.AddFormAction().None();
            builder.AddFontSrc().None();
            builder.AddStyleSrc().None();
            builder.AddScriptSrc().None();
            builder.AddScriptSrcElem().None();
            builder.AddBaseUri().Self();
            builder.AddFrameAncestors().None();
            builder.AddCustomDirective("require-trusted-types-for", "'script'");
        });

        if (!isDevelopment)
        {
            s_headerApiPolicy.AddStrictTransportSecurityMaxAgeIncludeSubDomainsAndPreload();
        }

        return s_headerApiPolicy;
    }
}
