using FacturacionElectronica.Client.Services;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
namespace FacturacionElectronica.Client.Auth
{
  public class BearerHandler : DelegatingHandler
  {
    private readonly IJSRuntime _js;
    public BearerHandler(IJSRuntime js) => _js = js;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
      var token = await _js.InvokeAsync<string?>("localStorage.getItem", AuthService.TokenKey);
      if (!string.IsNullOrWhiteSpace(token))
      {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
      }
      return await base.SendAsync(request, ct);
    }
  }
}
