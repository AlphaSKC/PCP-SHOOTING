using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace PCP_Shooting.Security;

// Supabase publica las claves de firma directamente en su endpoint JWKS.
public sealed class SupabaseJwksRetriever(string issuer) : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancellationToken)
    {
        var document = await retriever.GetDocumentAsync(address, cancellationToken);
        var configuration = new OpenIdConnectConfiguration { Issuer = issuer };
        foreach (var key in new JsonWebKeySet(document).GetSigningKeys())
        {
            configuration.SigningKeys.Add(key);
        }
        return configuration;
    }
}