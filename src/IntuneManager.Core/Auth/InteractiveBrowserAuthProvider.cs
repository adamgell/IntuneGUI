using Azure.Core;
using Azure.Identity;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Auth;

public class InteractiveBrowserAuthProvider : IAuthenticationProvider
{
    public Task<TokenCredential> GetCredentialAsync(TenantProfile profile, CancellationToken cancellationToken = default)
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(profile.Cloud);

        TokenCredential credential = profile.AuthMethod switch
        {
            AuthMethod.ClientSecret when !string.IsNullOrWhiteSpace(profile.ClientSecret) =>
                new ClientSecretCredential(
                    profile.TenantId,
                    profile.ClientId,
                    profile.ClientSecret,
                    new ClientSecretCredentialOptions { AuthorityHost = authorityHost }),

            AuthMethod.Interactive => new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = profile.TenantId,
                ClientId = profile.ClientId,
                AuthorityHost = authorityHost,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = $"IntuneManager-{profile.Id}"
                }
            }),

            AuthMethod.ClientSecret => throw new InvalidOperationException(
                "ClientSecret auth method requires a non-empty ClientSecret value."),

            _ => throw new NotSupportedException(
                $"AuthMethod '{profile.AuthMethod}' is not supported. Only Interactive and ClientSecret are implemented.")
        };

        return Task.FromResult(credential);
    }
}
