namespace ReelSoon.Infrastructure;

/// <summary>
/// Rewrites the host of Keycloak back-channel calls from the browser-facing authority to an
/// internally reachable base URL.
/// </summary>
/// <remarks>
/// In Docker the browser reaches Keycloak at something like <c>http://localhost:8081</c> while
/// the app container reaches the same server at <c>http://keycloak:8080</c>. The authority has
/// to stay the browser-facing value, because it appears in redirects and in the token's
/// <c>iss</c> claim, but discovery, token and userinfo requests are made by the app and must go
/// to the internal address.
/// </remarks>
public sealed class KeycloakBackchannelHandler : HttpClientHandler
{
    private readonly Uri authority;
    private readonly Uri backchannelBase;

    public KeycloakBackchannelHandler(string authority, string backchannelBaseUrl)
    {
        this.authority = new Uri(authority, UriKind.Absolute);
        backchannelBase = new Uri(backchannelBaseUrl, UriKind.Absolute);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null
            && string.Equals(request.RequestUri.Host, authority.Host, StringComparison.OrdinalIgnoreCase))
        {
            request.RequestUri = new UriBuilder(request.RequestUri)
            {
                Scheme = backchannelBase.Scheme,
                Host = backchannelBase.Host,
                Port = backchannelBase.Port,
            }.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
