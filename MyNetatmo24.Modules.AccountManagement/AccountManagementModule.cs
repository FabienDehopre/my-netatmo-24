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
using MyNetatmo24.Modules.AccountManagement.RateLimiting;
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

        // Registers the validation services for the DataAnnotations on the endpoint request
        // contracts of this module. The source generator behind AddValidation() only discovers
        // validatable types in the assembly the call sits in, so it has to be this one.
        builder.Services.AddValidation();

        // The abuse protection of the only anonymous endpoint of the module. The policy is declared
        // here, next to the endpoint it guards; the host owns the middleware that enforces it.
        builder.Services.AddRateLimiter(options =>
            options.AddPolicy<string, RegistrationRateLimiterPolicy>(RegistrationRateLimiterPolicy.Name));

        builder.Services.AddScoped<IUserRegistrationService, StubUserRegistrationService>();

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

        return builder;
    }

    public WebApplication UseModule(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("account")
            .RequireAuthorization()
            .WithTags("Account Management");
        EnsureAccount.Configure(group);
        MyAccount.Configure(group);
        Registration.Configure(group);
        RestoreAccount.Configure(group);

        return app;
    }
}
