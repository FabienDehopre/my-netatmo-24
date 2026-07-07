using MyNetatmo24.ApiService;

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
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("MyNetatmo24 API");
        // options.AddPreferredSecuritySchemes("Auth0");
        // options.AddAuthorizationCodeFlow("Auth0", flow =>
        // {
        //     flow.ClientId = builder.Configuration["Auth0:ClientId"];
        //     flow.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
        //     flow.Pkce = Pkce.Sha256;
        //     flow.SelectedScopes = ["openid", "profile", "email", "offline_access", "read:weatherdata"];
        //     flow.AddQueryParameter("audience", app.Configuration["Auth0:Audience"]!);
        //     flow.RedirectUri = "https://localhost:7115/scalar/";
        // });
    });
}

// Configure Aspire default endpoints
app.MapDefaultEndpoints();

await app.RunAsync();
