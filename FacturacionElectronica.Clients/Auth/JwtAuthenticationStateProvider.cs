// Ubicación: FacturacionElectronica.Clients/Auth/JwtAuthenticationStateProvider.cs
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace FacturacionElectronica.Clients.Auth
{
  public class JwtAuthenticationStateProvider : AuthenticationStateProvider
  {
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private const string TokenKey = "authToken";

    public JwtAuthenticationStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
    {
      _jsRuntime = jsRuntime;
      _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
      var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);

      if (string.IsNullOrEmpty(token))
      {
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
      }

      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
      return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt")));
    }

    public async Task NotifyUserAuthentication(string token)
    {
      var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
      var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
      NotifyAuthenticationStateChanged(authState);
    }

    public async Task NotifyUserLogout()
    {
      var identity = new ClaimsPrincipal(new ClaimsIdentity());
      var authState = Task.FromResult(new AuthenticationState(identity));
      NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
      var claims = new List<Claim>();
      var payload = jwt.Split('.')[1];
      var jsonBytes = ParseBase64WithoutPadding(payload);
      var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

      if (keyValuePairs != null)
      {
        keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles);

        if (roles != null)
        {
          if (roles.ToString().Trim().StartsWith("["))
          {
            var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString());
            foreach (var parsedRole in parsedRoles)
            {
              claims.Add(new Claim(ClaimTypes.Role, parsedRole));
            }
          }
          else
          {
            claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
          }
          keyValuePairs.Remove(ClaimTypes.Role);
        }

        claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));
      }
      return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
      switch (base64.Length % 4)
      {
        case 2: base64 += "=="; break;
        case 3: base64 += "="; break;
      }
      return Convert.FromBase64String(base64);
    }
  }
}
