using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OpenApiComponents = Microsoft.OpenApi.OpenApiComponents;
using OpenApiDocument = Microsoft.OpenApi.OpenApiDocument;
using OpenApiSecurityScheme = Microsoft.OpenApi.OpenApiSecurityScheme;

namespace MyNetatmo24.ApiService;

/// <summary>
/// The name of the OpenAPI security scheme, matching the authentication scheme registered by
/// <c>Auth0.AspNetCore.Authentication.Api</c> (which registers its JWT scheme as "Auth0").
/// </summary>
internal static class SecuritySchemeConstants
{
    public const string Auth0 = "Auth0";
}

/// <summary>
/// Adds the Auth0 OAuth2 (authorization code flow) security scheme definition to the OpenAPI document
/// components when the matching authentication scheme is registered. This lets Scalar render an
/// "Authenticate" button that drives the Auth0 login flow instead of a bare token input.
/// </summary>
internal sealed class Auth0SecuritySchemeDocumentTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IConfiguration configuration) : IOpenApiDocumentTransformer
{
    private static readonly Dictionary<string, string> s_scopes = new()
    {
        ["openid"] = "Sign in and read the user's identity.",
        ["profile"] = "Read the user's profile information.",
        ["email"] = "Read the user's email address.",
        ["offline_access"] = "Obtain a refresh token to keep the session alive.",
        ["read:weatherdata"] = "Read weather data.",
    };

    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider = authenticationSchemeProvider.ThrowIfNull();
    private readonly IConfiguration _configuration = configuration.ThrowIfNull();

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == SecuritySchemeConstants.Auth0))
        {
            var domain = _configuration["Auth0:Domain"].ThrowIfNullOrWhiteSpace();
            var scheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"https://{domain}/authorize"),
                        TokenUrl = new Uri($"https://{domain}/oauth/token"),
                        RefreshUrl = new Uri($"https://{domain}/oauth/token"),
                        Scopes = s_scopes,
                    },
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                [SecuritySchemeConstants.Auth0] = scheme
            };
        }
    }
}

/// <summary>
/// Marks each secured operation with the Auth0 security requirement. Operations that allow anonymous
/// access or that carry no authorization metadata are left untouched, so no lock icon is shown for them.
/// </summary>
internal sealed class Auth0SecurityRequirementOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();
        if (allowsAnonymous || !requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        var reference = new OpenApiSecuritySchemeReference(SecuritySchemeConstants.Auth0, context.Document);
        operation.Security = [new OpenApiSecurityRequirement { [reference] = [] }];
        return Task.CompletedTask;
    }
}
