// Ubicación: FacturacionElectronica.Clients/Auth/AuthService.cs
using FacturacionElectronica.Clients.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FacturacionElectronica.Clients.Auth
{
  public class AuthService
  {
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private const string TokenKey = "authToken";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, AuthenticationStateProvider authenticationStateProvider)
    {
      _httpClient = httpClient;
      _jsRuntime = jsRuntime;
      _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<string> LoginAsync(LoginRequestDto loginRequest)
    {
      var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

      if (!response.IsSuccessStatusCode)
      {
        throw new System.Exception("Error de autenticación: Credenciales inválidas o usuario inactivo.");
      }

      var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

      if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token) || string.IsNullOrEmpty(loginResponse.Rol))
      {
        throw new System.Exception("Respuesta de login inválida desde el servidor.");
      }

      await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, loginResponse.Token);
      await ((JwtAuthenticationStateProvider)_authenticationStateProvider).NotifyUserAuthentication(loginResponse.Token);

      return loginResponse.Rol;
    }

    public async Task LogoutAsync()
    {
      await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
      await ((JwtAuthenticationStateProvider)_authenticationStateProvider).NotifyUserLogout();
    }
  }
}
