using Auth0.ManagementApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// Puts an <see cref="IUserRegistrationService"/> behind the Registration endpoint.
/// </summary>
internal static class UserRegistrationExtensions
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Registers the identity-provider-backed <see cref="IUserRegistrationService"/> when this
        /// host holds machine-to-machine credentials for the Auth0 tenant, and the stub that
        /// creates nothing when it does not.
        /// </summary>
        /// <remarks>
        /// A host without credentials still has to start and still has to answer the Registration
        /// endpoint -- the tenant is provisioned once, by hand, and every other environment (a
        /// developer machine, a test host) runs without it.
        /// </remarks>
        /// <returns>The builder, so that the module configuration reads as one chain.</returns>
        public WebApplicationBuilder AddUserRegistration()
        {
            // The section is read once, here, and that one instance is what the container hands out:
            // which implementation is registered is decided from these very values, so a second
            // reader that could disagree with this one would decide nothing but confusion.
            var options = builder.Configuration.GetSection(Auth0ManagementOptions.SectionName)
                .Get<Auth0ManagementOptions>() ?? new Auth0ManagementOptions();
            builder.Services.AddSingleton(Options.Create(options));

            if (!options.IsConfigured)
            {
                // A developer machine runs without the tenant and gets the stub, which accepts
                // everything so the endpoint can be exercised. Anywhere else, nobody provisioned
                // the machine-to-machine application (or the secret went missing), and answering
                // success would send a real person off to wait for a verification e-mail that is
                // never coming -- so the honest answer is that registration is unavailable.
                if (builder.Environment.IsDevelopment())
                {
                    builder.Services.AddScoped<IUserRegistrationService, StubUserRegistrationService>();
                }
                else
                {
                    builder.Services.AddScoped<IUserRegistrationService, UnavailableUserRegistrationService>();
                }

                return builder;
            }

            // Both are singletons the container owns and disposes: the token provider caches the
            // machine-to-machine access token it hands out, which is pointless if it is rebuilt per
            // request, and the client owns the HttpClient it talks to Auth0 over.
            //
            // That HttpClient deliberately does not come from IHttpClientFactory, unlike the one
            // the user-info service next door uses: the factory defaults of this application add
            // the standard resilience handler, which retries a failed request -- and retrying a
            // create-user call that timed out after Auth0 had already honoured it would answer the
            // prospective user with a conflict over the identity they just got.
            builder.Services.AddSingleton(_ => new ClientCredentialsTokenProvider(
                options.Domain!,
                options.ClientId!,
                options.ClientSecret!));
            // Registered once, as the interface only: a second registration forwarding to the first
            // would put the one disposable client in the container's disposal list twice.
            builder.Services.AddSingleton<IManagementApiClient>(sp => new ManagementClient(new ManagementClientOptions
            {
                Domain = options.Domain!,
                TokenProvider = sp.GetRequiredService<ClientCredentialsTokenProvider>(),
            }));
            builder.Services.AddScoped<IUserRegistrationService, Auth0UserRegistrationService>();

            return builder;
        }
    }
}
