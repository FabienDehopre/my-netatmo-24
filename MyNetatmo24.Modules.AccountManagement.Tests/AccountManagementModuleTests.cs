using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Infrastructure;

namespace MyNetatmo24.Modules.AccountManagement.Tests;

public class AccountManagementModuleTests
{
    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Constants.DatabaseName}"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["Auth0:Domain"] = "tenant.auth0.com",
        });
        // Normally provided by the host (ServiceDefaults); the module's AccountDbContext depends on it.
        builder.Services.AddSingleton(TimeProvider.System);
        return builder;
    }

    [Test]
    public async Task AddModule_ReturnsSameBuilder()
    {
        var builder = CreateBuilder();

        var result = new AccountManagementModule().AddModule(builder);

        await Assert.That(result).IsSameReferenceAs(builder);
    }

    [Test]
    public async Task AddModule_RegistersModuleServices()
    {
        var builder = CreateBuilder();

        new AccountManagementModule().AddModule(builder);
        await using var provider = builder.Services.BuildServiceProvider();

        await Assert.That(provider.GetService<AccountDbContext>()).IsNotNull();
        await Assert.That(provider.GetService<IQueryable<MyNetatmo24.Modules.AccountManagement.Domain.Account>>()).IsNotNull();
        await Assert.That(provider.GetService<IUserInfoService>()).IsNotNull();
        await Assert.That(provider.GetKeyedService<HybridCache>("Account")).IsNotNull();
    }

    [Test]
    public async Task AddModule_WithNullBuilder_Throws()
    {
        await Assert.That(() => new AccountManagementModule().AddModule(null!))
            .Throws<ArgumentNullException>();
    }
}
