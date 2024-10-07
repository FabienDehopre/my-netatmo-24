using System.Security.Claims;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

var app = builder.Build();

// Configure Aspire default endpoints
app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints()
    .UseSwaggerGen();

app.Run();


