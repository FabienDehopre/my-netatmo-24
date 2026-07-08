using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OpenApiComponents = Microsoft.OpenApi.OpenApiComponents;
using OpenApiDocument = Microsoft.OpenApi.OpenApiDocument;
using OpenApiSecurityScheme = Microsoft.OpenApi.OpenApiSecurityScheme;

namespace MyNetatmo24.ApiService;

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider = authenticationSchemeProvider.ThrowIfNull();

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Auth0"))
        {
            var scheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token",
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Auth0"] = scheme
            };

            // Reference the scheme so operations are marked as secured (adds the lock icon).
            var reference = new OpenApiSecuritySchemeReference("Auth0", document);
            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations!.Values))
            {
                operation.Security = [new OpenApiSecurityRequirement { [reference] = [] }];
            }
        }
    }
}
