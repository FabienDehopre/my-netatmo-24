using MyNetatmo24.ApiService;
using MyNetatmo24.ApiService.Endpoints;
using MyNetatmo24.SharedKernel.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSecurity();
builder.AddAuthentication();
builder.AddOpenApi();
builder.AddErrorHandling();
builder.AddCaching();
// AddFeatureFlags ??
builder.AddWolverine();
builder.AddModules();

var app = builder.Build();

app.UseStatusCodePages();
app.UseExceptionHandler(
    new ExceptionHandlerOptions
    {
        SuppressDiagnosticsCallback = context => context.Exception is BadHttpRequestException
    }
);
app.UseAuthentication();
app.UseAuthorization();
app.UseModules();
// temporary until demo endpoint is removed
GetWeatherForecastEndpoint.Configure(app);
// end of temporary endpoints
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("MyNetatmo24 API");
        options.AddPreferredSecuritySchemes("Auth0");
        options.AddAuthorizationCodeFlow("Auth0", flow =>
        {
            flow.WithClientId(builder.Configuration["Auth0:ScalarClientId"])
                .WithPkce(Pkce.Sha256)
                .WithSelectedScopes("openid", "profile", "email", "offline_access", "read:weatherdata");
                // .WithCredentialsLocation(CredentialsLocation.Body)
                // .WithRedirectUri("https://localhost:7115/scalar/");
            var audience = builder.Configuration["Auth0:Audience"];
            if (!string.IsNullOrEmpty(audience))
            {
                flow.AddQueryParameter("audience", audience);
            }
        });
    });
}

// Configure Aspire default endpoints
app.MapDefaultEndpoints();

await app.RunAsync();
