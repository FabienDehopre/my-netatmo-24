using System.Security.Claims;
using FastEndpoints;
using FastEndpoints.ClientGen.Kiota;
using FastEndpoints.Swagger;
using Kiota.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MyNetatmo24.Backend.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure Aspire services
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<MyNetatmo24DbContext>("my-netatmo-24-db");

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
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://auth.dehopre.dev/";
    options.Audience = "https://my-netatmo24-api";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = ClaimTypes.NameIdentifier,
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadWeather", b =>
    {
        b.RequireAuthenticatedUser()
            .RequireClaim("permissions", "read:weatherdata");
    });
});
builder.Services.AddOutputCache();

var app = builder.Build();

// Configure Aspire default endpoints
app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseFastEndpoints()
    .UseSwaggerGen();

if (app.Environment.IsDevelopment())
{
    app.MapApiClientEndpoint("/cs-client", client =>
    {
        client.SwaggerDocumentName = "My Netatmo 24 API (v1)";
        client.Language = GenerationLanguage.CSharp;
        client.ClientNamespaceName = "MyNetatmo24";
        client.ClientClassName = "MyNetatmo24Client";
    }, options =>
    {
        options.AllowAnonymous();
        options.CacheOutput(p => p.Expire(TimeSpan.FromMinutes(5)));
        options.ExcludeFromDescription();
    });
    app.MapApiClientEndpoint("/ts-client", client =>
    {
        client.SwaggerDocumentName = "My Netatmo 24 API (v1)";
        client.Language = GenerationLanguage.TypeScript;
        client.ClientNamespaceName = "MyNetatmo24";
        client.ClientClassName = "MyNetatmo24Client";
    }, options =>
    {
        options.AllowAnonymous();
        options.CacheOutput(p => p.Expire(TimeSpan.FromMinutes(5)));
        options.ExcludeFromDescription();
    });
}

app.Run();


