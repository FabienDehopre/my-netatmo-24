using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Endpoints;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Modules;
using Wolverine.Attributes;
using ZiggyCreatures.Caching.Fusion;

[assembly: WolverineModule]

namespace MyNetatmo24.Modules.AccountManagement;

[UsedImplicitly]
public sealed class AccountManagementModule : IModule
{
    public WebApplicationBuilder AddModule(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<AccountDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString(Constants.DatabaseName)));
        builder.EnrichNpgsqlDbContext<AccountDbContext>(config => config.DisableRetry = true);
        builder.Services.AddTransient(sp => sp.GetRequiredService<AccountDbContext>().Set<Account>().AsNoTracking());

        builder.Services.AddFusionCache("Account")
            .TryWithAutoSetup()
            .WithCacheKeyPrefixByCacheName()
            .AsKeyedHybridCacheByCacheName();

        builder.Services.AddHttpContextAccessor();
        builder.Services
            .AddHttpClient<IUserInfoService, UserInfoService>("Auth0", (sp, client) =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var token = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                var domain = builder.Configuration["Auth0:Domain"];

                client.BaseAddress = new Uri($"https://{domain}/");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", token);
                }
            });

        AddRegistration(builder);

        // Turns DataAnnotations on this module's request types into 400 validation ProblemDetails.
        // The call has to live here rather than in the host: the validation source generator only sees
        // the [ValidatableType] types of the compilation it intercepts AddValidation() in.
        builder.Services.AddValidation();

        builder.Services.AddRateLimiter(Register.ConfigureRateLimiting);

        return builder;
    }

    /// <summary>
    /// Wires the registration seam to the Auth0 Management API. Until the owner provisions the
    /// machine-to-machine application the credentials are placeholders, so development falls back to the
    /// stub; anywhere else that fallback would silently swallow registrations, and startup fails instead.
    /// </summary>
    private static void AddRegistration(WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(Auth0RegistrationOptions.SectionName);
        var options = section.Get<Auth0RegistrationOptions>() ?? new Auth0RegistrationOptions();
        var stubIsAcceptable = builder.Environment.IsDevelopment() ||
                               builder.Environment.IsEnvironment(Constants.IntegrationTestEnvironmentName) ||
                               HostingContext.IsGeneratingOpenApiDocument;

        // Validated on start rather than here, so that the failure names the environment that is actually
        // serving traffic instead of breaking every build of the module.
        builder.Services.AddOptions<Auth0RegistrationOptions>()
            .Bind(section)
            .Validate(
                o => o.HasManagementCredentials || stubIsAcceptable,
                $"No Auth0 management credentials configured. Set {Auth0RegistrationOptions.SectionName}:ManagementClientId " +
                $"and {Auth0RegistrationOptions.SectionName}:ManagementClientSecret, or registration would accept every " +
                "request without creating anything.")
            .ValidateOnStart();

        if (options.HasManagementCredentials)
        {
            builder.Services.AddSingleton<IRegistrationService, Auth0RegistrationService>();
        }
        else
        {
            builder.Services.AddSingleton<IRegistrationService, StubRegistrationService>();
        }
    }

    public WebApplication UseModule(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("account")
            .RequireAuthorization()
            .WithTags("Account Management");
        EnsureAccount.Configure(group);
        MyAccount.Configure(group);
        Register.Configure(group);
        RestoreAccount.Configure(group);

        return app;
    }
}
