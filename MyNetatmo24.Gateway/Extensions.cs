using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MyNetatmo24.Gateway.Transformers;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace MyNetatmo24.Gateway;

internal static class Extensions
{
    /// <summary>
    /// The pseudo-policy name YARP understands as "this route requires no authorization at all",
    /// which it turns into <see cref="AllowAnonymousAttribute"/> metadata on the proxied endpoint.
    /// </summary>
    private const string AnonymousAuthorizationPolicy = "Anonymous";

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddReverseProxy()
        {
            builder.Services.AddSingleton<AddBearerTokenToHeadersTransform>();
            builder.Services.AddSingleton<AddAntiforgeryTokenResponseTransform>();
            builder.Services.AddSingleton<ValidateAntiforgeryTokenRequestTransform>();

            builder.Services
                .AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                .AddTransforms(builderContext =>
                {
                    builderContext.ResponseTransforms.Add(builderContext.Services
                        .GetRequiredService<AddAntiforgeryTokenResponseTransform>());
                    builderContext.RequestTransforms.Add(builderContext.Services
                        .GetRequiredService<ValidateAntiforgeryTokenRequestTransform>());
                    builderContext.RequestTransforms.Add(new RequestHeaderRemoveTransform("Cookie"));

                    // Only a route that demands an authenticated user may carry that user's access
                    // token onwards. A route without a policy, or one that opted out with YARP's
                    // "Anonymous" pseudo-policy, must reach the API with no Authorization header --
                    // even when the caller happens to be authenticated already.
                    if (RequiresAuthenticatedUser(builderContext.Route))
                    {
                        builderContext.RequestTransforms.Add(builderContext.Services
                            .GetRequiredService<AddBearerTokenToHeadersTransform>());
                    }
                })
                .AddServiceDiscoveryDestinationResolver();

            return builder;
        }

        public IHostApplicationBuilder AddAuthenticationSchemes()
        {
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = "__MyNetatmo24";
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
                    options.ClientId = builder.Configuration["Auth0:ClientId"];
                    options.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.ResponseMode = OpenIdConnectResponseMode.Query;

                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.MapInboundClaims = false;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("email");
                    options.Scope.Add("profile");
                    options.Scope.Add("read:weatherdata");

                    // Add this scope if you want to receive refresh tokens
                    options.Scope.Add("offline_access");

                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProviderForSignOut = context =>
                        {
                            var logoutUri =
                                $"https://{builder.Configuration.GetValue<string>("Auth0:Domain")}/oidc/logout?client_id={builder.Configuration.GetValue<string>("Auth0:ClientId")}";
                            var redirectUri = context.HttpContext.BuildRedirectUrl(context.Properties.RedirectUri);
                            logoutUri += $"&post_logout_redirect_uri={redirectUri}";

                            context.Response.Redirect(logoutUri);
                            context.HandleResponse();
                            return Task.CompletedTask;
                        },
                        OnRedirectToIdentityProvider = context =>
                        {
                            // Auth0 specific parameter to specify the audience
                            context.ProtocolMessage.SetParameter("audience",
                                builder.Configuration.GetValue<string>("Auth0:Audience"));
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services
                .AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());

            return builder;
        }

        public IHostApplicationBuilder AddRateLimiting()
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("user-or-ip", httpContext =>
                {
                    var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                        ? httpContext.User.FindFirstValue("name") ?? "anonymous"
                        : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                options.AddPolicy("otelcollector-ip", httpContext =>
                {
                    var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return builder;
        }
    }

    extension(HttpContext context)
    {
        public string BuildRedirectUrl(string? redirectUrl)
        {
            if (string.IsNullOrEmpty(redirectUrl)
                || !redirectUrl.StartsWith('/')
                || redirectUrl.StartsWith("//", StringComparison.Ordinal)
                || redirectUrl.Contains('\\', StringComparison.Ordinal))
            {
                redirectUrl = "/";
            }

            var request = context.Request;

            // Guard against Host header injection (CWE-601): never echo the raw
            // incoming Host into a redirect URL. Only reflect a host that is
            // explicitly allow-listed; otherwise fall back to a known-good host.
            var allowedHosts = context.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetValue<string>("AllowedHosts")?
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(host => !string.Equals(host, "*", StringComparison.Ordinal))
                .ToArray() ?? [];

            var matchedHost = allowedHosts
                .FirstOrDefault(host => string.Equals(host, request.Host.Host, StringComparison.OrdinalIgnoreCase));

            // Preserve the request port only when the host itself is allow-listed (e.g. local
            // dev on localhost:<port>); never carry an attacker-controlled port onto a fallback host.
            var authority = matchedHost is not null
                ? request.Host.Port is { } port ? $"{matchedHost}:{port}" : matchedHost
                : allowedHosts.FirstOrDefault() ?? "localhost";

            // Collapse the scheme to a known constant so a forwarded proto cannot be reflected.
            var scheme = string.Equals(request.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";

            // The path base is never echoed back either: it is derived from the request (and from
            // forwarded prefix headers when a proxy sets them), so it is remote input just like the
            // host. The gateway is always mounted at the root, so an empty path base is correct.
            return $"{scheme}://{authority}{redirectUrl}";
        }
    }

    /// <summary>
    /// Tells whether a proxied route is one the caller must be authenticated for, and therefore one
    /// the access token of the authenticated user belongs on.
    /// </summary>
    /// <param name="route">The route configuration to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when the route names an authorization policy other than the anonymous
    /// pseudo-policy; otherwise <see langword="false"/>.
    /// </returns>
    private static bool RequiresAuthenticatedUser(RouteConfig route) =>
        !string.IsNullOrEmpty(route.AuthorizationPolicy)
        && !string.Equals(route.AuthorizationPolicy, AnonymousAuthorizationPolicy,
            StringComparison.OrdinalIgnoreCase);
}
