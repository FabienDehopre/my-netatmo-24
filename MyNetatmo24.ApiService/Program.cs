var builder = WebApplication.CreateBuilder(args);

// Configure Aspire services
builder.AddServiceDefaults();

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
builder.Services.AddOutputCache();

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
    options.Policies.UseDurableLocalQueues();
    options.Policies.AutoApplyTransactions();
});

builder.AddModules();

var app = builder.Build();

app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(config => config.Errors.UseProblemDetails());

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options.AddPreferredSecuritySchemes("Auth0");
        options.AddAuthorizationCodeFlow("Auth0", flow =>
        {
            flow.ClientId = builder.Configuration["Auth0:ClientId"];
            flow.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
            flow.Pkce = Pkce.Sha256;
            flow.SelectedScopes = ["openid", "profile", "email", "offline_access", "read:weatherdata"];
            flow.AddQueryParameter("audience", app.Configuration["Auth0:Audience"]!);
        });
    });
}

// Configure Aspire default endpoints
app.MapDefaultEndpoints();

app.Run();
