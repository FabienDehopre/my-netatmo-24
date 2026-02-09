var builder = WebApplication.CreateBuilder(args);

// Configure Aspire services
builder.AddServiceDefaults();

builder.Services
    .AddFastEndpoints()
    .SwaggerDocument(options =>
    {
        options.RemoveEmptyRequestSchema = true;
        options.EnableJWTBearerAuth = false;
        options.DocumentSettings = settings =>
        {
            settings.Title = "My Netatmo 24 API";
            settings.Description = "An API to access weather data from Netatmo devices.";
            settings.MarkNonNullablePropsAsRequired();
            settings.AddAuth("Auth0", new()
            {
                Type = OpenApiSecuritySchemeType.OAuth2,
                BearerFormat =  "JWT",
                Scheme = "Bearer",
                In =  OpenApiSecurityApiKeyLocation.Header,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new()
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
    options.JwtBearerOptions = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions
    {
        Audience = builder.Configuration["Auth0:Audience"],
        TokenValidationParameters = new()
        {
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = "permissions",
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadWeather", b =>
    {
        b.RequireAuthenticatedUser()
            .RequireRole("read:weatherdata");
    });
});
builder.Services.AddOutputCache();

var app = builder.Build();

app.UseOutputCache();

// Configure Aspire default endpoints
app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();

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

app.Run();


