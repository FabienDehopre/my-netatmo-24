using JasperFx.Resources;
using MartinCostello.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;
using JsonSchemaType = Microsoft.OpenApi.JsonSchemaType;
using OpenApiContact = Microsoft.OpenApi.OpenApiContact;
using OpenApiInfo = Microsoft.OpenApi.OpenApiInfo;
using OpenApiServer = Microsoft.OpenApi.OpenApiServer;

namespace MyNetatmo24.ApiService;

public static class Extensions
{
    private static HeaderPolicyCollection? s_headerApiPolicy;
    private static HeaderPolicyCollection? s_headerScalarPolicy;

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddAuthentication()
        {
            // Add authentication and authorization using Auth0
            builder.Services.AddAuth0ApiAuthentication(
                builder.Configuration.GetSection("Auth0"),
                configureJwtBearer: jwt =>
                {
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = "permissions",
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

        public WebApplicationBuilder AddOpenApi()
        {
            builder.Services.AddOpenApi(openApi =>
            {
                openApi.CreateSchemaReferenceId = (jsonTypeInfo) =>
                {
                    var schemaRefId = OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo);
                    if (schemaRefId is null || jsonTypeInfo?.Type?.FullName is null)
                    {
                        return null;
                    }

                    return jsonTypeInfo.Type.FullName.Replace("+", ".", StringComparison.Ordinal);
                };

                openApi.AddDocumentTransformer((document, _, _) =>
                {
                    document.Servers = [new OpenApiServer { Url = "https://localhost:7115" }];
                    document.Info = new OpenApiInfo
                    {
                        Title = "My Netatmo 24 API",
                        Version = "v1",
                        Description = "API for My Netatmo 24 application",
                        Contact = new OpenApiContact
                        {
                            Name = "Support",
                            // Email = "my-netatmo-24@dehopre.dev",
                            Url = new Uri("https://github.com/FabienDehopre/my-netatmo-24"),
                        }
                    };

                    return Task.CompletedTask;
                });
                openApi.AddDocumentTransformer<Auth0SecuritySchemeDocumentTransformer>();
                openApi.AddOperationTransformer<Auth0SecurityRequirementOperationTransformer>();
                openApi.AddSchemaTransformer((schema, _, _) =>
                {
                    // JsonNumberHandling.AllowReadingFromString (the ASP.NET Core default) makes numeric
                    // properties emit type ["integer"/"number", "string"], which Kiota cannot map to a
                    // numeric type (it falls back to UntypedNode). Advertise only the numeric type.
                    const JsonSchemaType numeric = JsonSchemaType.Integer | JsonSchemaType.Number;
                    if (schema.Type is { } type && (type & numeric) != 0 && (type & JsonSchemaType.String) != 0)
                    {
                        schema.Type = type & ~JsonSchemaType.String;
                        schema.Pattern = null;
                    }

                    return Task.CompletedTask;
                });
            });

            builder.Services.AddOpenApiExtensions(openApi =>
            {
                openApi.AddServerUrls = true;
                openApi.DefaultServerUrl = "https://localhost:7115";
                // openApi.AddExamples = true;
                // openApi.SerializationContexts.Add(TODO);
                // openApi.AddExample<ProblemDetails, ProblemDetailsExampleProvider>();
                openApi.AddXmlComments<Program>();
            });

            return builder;
        }

        public WebApplicationBuilder AddRateLimiting()
        {
            // Modules declare their own policies; the host only settles what a rejection looks like.
            // 429 rather than the framework default of 503: the caller was throttled, not failed.
            builder.Services.AddRateLimiter(options =>
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);

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
            builder.AddRedisDistributedCache(connectionName: Constants.CacheName);
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
                    options.Configuration = builder.Configuration.GetConnectionString(Constants.CacheName);
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

        public WebApplicationBuilder AddSecurity()
        {
            var isDevelopment = builder.Environment.IsDevelopment();
            builder.Services.AddSecurityHeaderPolicies()
                .SetPolicySelector(context =>
                {
                    // Scalar UI (development only) needs a relaxed CSP to load its scripts/styles.
                    if (isDevelopment &&
                        context.HttpContext.Request.Path.StartsWithSegments(
                            "/scalar", StringComparison.OrdinalIgnoreCase))
                    {
                        var auth0Domain = builder.Configuration["Auth0:Domain"];
                        return GetScalarHeaderPolicyCollection(auth0Domain);
                    }

                    return GetApiHeaderPolicyCollection(isDevelopment);
                });

            return builder;
        }
    }

    internal static HeaderPolicyCollection GetApiHeaderPolicyCollection(bool isDevelopment)
    {
        if (s_headerApiPolicy is not null)
        {
            return s_headerApiPolicy;
        }

        // Build the whole policy in a local variable and only publish it to the static
        // field once fully constructed. Publishing a partially-built collection (e.g.
        // assigning to s_headerApiPolicy before the CSP/HSTS directives are added) lets a
        // concurrent request read the field and enumerate it while this thread is still
        // mutating it, causing "Collection was modified; enumeration operation may not
        // execute." under parallel test/request execution.
        var policy = new HeaderPolicyCollection()
            .AddFrameOptionsDeny()
            .AddContentTypeOptionsNoSniff()
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .AddCrossOriginOpenerPolicy(builder => builder.SameOrigin())
            .AddCrossOriginEmbedderPolicy(builder => builder.RequireCorp())
            .AddCrossOriginResourcePolicy(builder => builder.SameOrigin())
            .RemoveServerHeader()
            .AddPermissionsPolicyWithDefaultSecureDirectives();

        policy.AddContentSecurityPolicy(builder =>
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
            policy.AddStrictTransportSecurityMaxAgeIncludeSubDomainsAndPreload();
        }

        s_headerApiPolicy = policy;
        return s_headerApiPolicy;
    }

    // Relaxed policy for the Scalar API reference UI (development only). Scalar loads its
    // own bundled scripts (scalar.js, scalar.aspnetcore.js) and an inline bootstrap script,
    // which the strict API CSP blocks via `script-src-elem 'none'` and trusted types.
    internal static HeaderPolicyCollection GetScalarHeaderPolicyCollection(string? auth0Domain)
    {
        if (s_headerScalarPolicy is not null)
        {
            return s_headerScalarPolicy;
        }

        // See the comment in GetApiHeaderPolicyCollection: build fully before publishing.
        var policy = new HeaderPolicyCollection()
            .AddFrameOptionsDeny()
            .AddContentTypeOptionsNoSniff()
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .AddCrossOriginOpenerPolicy(builder => builder.UnsafeNone())
            .AddCrossOriginResourcePolicy(builder => builder.SameOrigin())
            .RemoveServerHeader();

        policy.AddContentSecurityPolicy(builder =>
        {
            builder.AddObjectSrc().None();
            builder.AddBlockAllMixedContent();
            builder.AddDefaultSrc().Self();
            builder.AddScriptSrc().Self().UnsafeInline().UnsafeEval();
            builder.AddScriptSrcElem().Self().UnsafeInline();
            builder.AddStyleSrc().Self().UnsafeInline();
            builder.AddImgSrc().Self().Data().Blob();
            builder.AddFontSrc().Self().From("fonts.scalar.com").Data();
            var connectSrc = builder.AddConnectSrc().Self().From("api.scalar.com");
            if (!string.IsNullOrWhiteSpace(auth0Domain))
            {
                connectSrc.From(auth0Domain);
            }

            builder.AddBaseUri().Self();
            builder.AddFrameAncestors().None();
        });

        s_headerScalarPolicy = policy;
        return s_headerScalarPolicy;
    }
}
