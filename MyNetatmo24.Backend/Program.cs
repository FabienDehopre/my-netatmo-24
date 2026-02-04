// using MyNetatmo24.Infrastructure.Data;

using Auth0.AspNetCore.Authentication.Api;

var builder = WebApplication.CreateBuilder(args);

// Configure Aspire services
builder.AddServiceDefaults();

builder.Services
    .AddFastEndpoints()
    .SwaggerDocument(options =>
    {
        options.RemoveEmptyRequestSchema = true;
        options.DocumentSettings = settings =>
        {
            settings.Title = "My Netatmo 24 API";
            settings.Version = "v1";
            settings.DocumentName = "My Netatmo 24 API (v1)";
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
            // .RequireClaim("scope", "read:weatherdata");
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
app.UseFastEndpoints()
    .UseSwaggerGen(options =>
    {
        options.Path = "/openapi/{documentName}.yaml";
    });

app.Run();


