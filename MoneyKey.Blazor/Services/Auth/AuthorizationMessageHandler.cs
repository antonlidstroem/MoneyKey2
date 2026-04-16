using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;
using MoneyKey.Blazor.Services.Auth;

namespace MoneyKey.Blazor.Services.Auth;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IServiceProvider _sp;
    private bool _refreshing;

    public AuthorizationMessageHandler(IServiceProvider sp) => _sp = sp;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Content != null)
            await request.Content.LoadIntoBufferAsync();

        if (!string.IsNullOrEmpty(JwtAuthenticationStateProvider.AccessToken))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtAuthenticationStateProvider.AccessToken);

        var response = await base.SendAsync(request, ct);

        var isAuthEndpoint = request.RequestUri?.AbsolutePath
            .Contains("/api/auth/", StringComparison.OrdinalIgnoreCase) ?? false;

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && !isAuthEndpoint && !_refreshing)
        {
            _refreshing = true;
            try
            {
                var provider = (JwtAuthenticationStateProvider)_sp
                    .GetRequiredService<AuthenticationStateProvider>();
                await provider.GetAuthenticationStateAsync();

                if (!string.IsNullOrEmpty(JwtAuthenticationStateProvider.AccessToken))
                {
                    var retry = await CloneRequestAsync(request);
                    retry.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", JwtAuthenticationStateProvider.AccessToken);
                    response = await base.SendAsync(retry, ct);
                }
            }
            finally { _refreshing = false; }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage src)
    {
        var clone = new HttpRequestMessage(src.Method, src.RequestUri);
        foreach (var h in src.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        if (src.Content != null)
        {
            var ms = new MemoryStream();
            await src.Content.CopyToAsync(ms);
            ms.Position   = 0;
            clone.Content = new StreamContent(ms);
            foreach (var h in src.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        return clone;
    }
}
