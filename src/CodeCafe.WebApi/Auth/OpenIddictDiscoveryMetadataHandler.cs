using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CodeCafe.WebApi.Auth;

internal sealed class OpenIddictDiscoveryMetadataHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleConfigurationRequestContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.HandleConfigurationRequestContext>()
            .UseSingletonHandler<OpenIddictDiscoveryMetadataHandler>()
            .SetOrder(OpenIddictServerHandlers.Discovery.AttachAdditionalMetadata.Descriptor.Order + 500)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    public ValueTask HandleAsync(OpenIddictServerEvents.HandleConfigurationRequestContext context)
    {
        context.TokenEndpointAuthenticationMethods.Add(ClientAuthenticationMethods.None);
        if (context.Issuer is not null)
        {
            context.Metadata["registration_endpoint"] = new Uri(context.Issuer, "/connect/register").AbsoluteUri;
        }

        return ValueTask.CompletedTask;
    }
}
