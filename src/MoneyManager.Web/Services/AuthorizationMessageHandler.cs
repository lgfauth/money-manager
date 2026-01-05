using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace MoneyManager.Web.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";

    public AuthorizationMessageHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", TokenKey, cancellationToken);
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"[AuthHandler] Token adicionado ao header: Bearer {token.Substring(0, Math.Min(20, token.Length))}...");
            }
            else
            {
                Console.WriteLine("[AuthHandler] Nenhum token encontrado no sessionStorage");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthHandler] Erro ao obter token: {ex.Message}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
