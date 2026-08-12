using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Infrastructure;

namespace MyNetatmo24.Modules.AccountManagement.Tests;

public class AccountManagementModuleTests
{
    private static WebApplicationBuilder CreateBuilder(
        bool withManagementCredentials = false,
        string? environmentName = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName ?? Environments.Development,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Constants.DatabaseName}"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["Auth0:Domain"] = "tenant.auth0.com",
        });
        if (withManagementCredentials)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{Auth0ManagementOptions.SectionName}:Domain"] = "tenant.eu.auth0.com",
                [$"{Auth0ManagementOptions.SectionName}:ClientId"] = "client-id",
                [$"{Auth0ManagementOptions.SectionName}:ClientSecret"] = "client-secret",
            });
        }

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
        await Assert.That(provider.GetService<IUserRegistrationService>()).IsNotNull();
        await Assert.That(provider.GetKeyedService<HybridCache>("Account")).IsNotNull();
    }

    [Test]
    public async Task AddModule_WithoutManagementCredentialsInDevelopment_RegistersTheStubRegistrationService()
    {
        var builder = CreateBuilder();

        new AccountManagementModule().AddModule(builder);
        await using var provider = builder.Services.BuildServiceProvider();

        // A developer machine with no machine-to-machine application provisioned still has to
        // start and still has to answer the Registration endpoint.
        await Assert.That(provider.GetService<IUserRegistrationService>()).IsTypeOf<StubUserRegistrationService>();
    }

    [Test]
    public async Task AddModule_WithoutManagementCredentialsOutsideDevelopment_RegistersTheUnavailableRegistrationService()
    {
        var builder = CreateBuilder(environmentName: Environments.Production);

        new AccountManagementModule().AddModule(builder);
        await using var provider = builder.Services.BuildServiceProvider();

        // Where real people register, missing credentials have to be reported as such: the success
        // the stub answers would promise a verification e-mail nobody is going to send.
        await Assert.That(provider.GetService<IUserRegistrationService>())
            .IsTypeOf<UnavailableUserRegistrationService>();
    }

    [Test]
    public async Task AddModule_WithManagementCredentials_RegistersTheIdentityProviderBackedRegistrationService()
    {
        var builder = CreateBuilder(withManagementCredentials: true);

        new AccountManagementModule().AddModule(builder);
        await using var provider = builder.Services.BuildServiceProvider();

        await Assert.That(provider.GetService<IUserRegistrationService>()).IsTypeOf<Auth0UserRegistrationService>();
    }

    [Test]
    public async Task AddModule_WithNullBuilder_Throws()
    {
        await Assert.That(() => new AccountManagementModule().AddModule(null!))
            .Throws<ArgumentNullException>();
    }
}
