using MyNetatmo24.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSecurity();
builder.AddAuthentication();
builder.AddErrorHandling(); // TODO: check that this is compatible with FastEndpoints error handling
builder.AddCaching();
builder.AddWolverine();
builder.AddFastEndpointsWithOpenApi();
builder.AddModules();

var app = builder.Build();

app.UseStatusCodePages();
app.UseExceptionHandler(); // or app.UseDefaultExceptionHandler(); // default way to handler exception in FastEndpoints
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(); // or app.UseFastEndpoints(config => config.Errors.UseProblemDetails()); // when using default exception handler
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("MyNetatmo24 API");
        options.AddPreferredSecuritySchemes("Auth0");
        options.AddAuthorizationCodeFlow("Auth0", flow =>
        {
            flow.ClientId = builder.Configuration["Auth0:ClientId"];
            flow.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
            flow.Pkce = Pkce.Sha256;
            flow.SelectedScopes = ["openid", "profile", "email", "offline_access", "read:weatherdata"];
            flow.AddQueryParameter("audience", app.Configuration["Auth0:Audience"]!);
            flow.RedirectUri = "https://localhost:7115/scalar/";
        });
    });
}

// Configure Aspire default endpoints
app.MapDefaultEndpoints();

app.Run();
