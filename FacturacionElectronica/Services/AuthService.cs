using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
namespace FacturacionElectronica.Client.Services
{
  public class AuthService
  {

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly AuthenticationStateProvider _provider;

    public const string TokenKey = "jwt_token";

    public AuthService(HttpClient http, IJSRuntime js, AuthenticationStateProvider provider)
    {
      _http = http;
      _js = js;
      _provider = provider;
    }

    public async Task<bool> LoginAsync(string correo, string password)
    {
      var payload = new { correo, contrasena = password };
      var resp = await _http.PostAsJsonAsync("api/auth/login", payload);

      if (!resp.IsSuccessStatusCode) return false;

      var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
      if (body is null || string.IsNullOrWhiteSpace(body.Token)) return false;

      // guarda token
      await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, body.Token);

      // notifica provider
      if (_provider is JwtAuthenticationStateProvider jwtProvider)
        jwtProvider.NotifyUserAuthentication(body.Token);

      return true;
    }

    public async Task LogoutAsync()
    {
      await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
      if (_provider is JwtAuthenticationStateProvider jwtProvider)
        jwtProvider.NotifyUserLogout();
    }

    public class LoginResponse
    {
      public string Token { get; set; } = "";
      public DateTime Expira { get; set; }
      public string NombreCompleto { get; set; } = "";
      public string Rol { get; set; } = "";
    }
  }
}
