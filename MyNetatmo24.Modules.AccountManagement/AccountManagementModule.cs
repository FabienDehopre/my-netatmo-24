using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Modules;
using Wolverine.Attributes;

[assembly: WolverineModule]

namespace MyNetatmo24.Modules.AccountManagement;

public sealed class AccountManagementModule : IModule
{
    public WebApplicationBuilder AddModule(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<AccountDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString(Constants.DatabaseName)));
        builder.EnrichNpgsqlDbContext<AccountDbContext>(config => config.DisableRetry = true);
        builder.Services.AddTransient(sp => sp.GetRequiredService<AccountDbContext>().Set<Account>().AsNoTracking());

        builder.Services.AddHttpContextAccessor();
        builder.Services
            .AddHttpClient<IUserInfoService, UserInfoService>("Auth0", (sp, client) =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var token = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

                client.BaseAddress = new Uri("https://auth.dehopre.dev/");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", token);
                }
            });

        return builder;
    }
}
