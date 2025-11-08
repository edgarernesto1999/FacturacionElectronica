using FacturacionElectronica.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace FacturacionElectronica.Client.Auth
{
  public class JwtAuthenticationStateProvider : AuthenticationStateProvider
  {
    private readonly IJSRuntime _js;
    public JwtAuthenticationStateProvider(IJSRuntime js) => _js = js;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
      var token = await _js.InvokeAsync<string?>("localStorage.getItem", AuthService.TokenKey);

      if (string.IsNullOrWhiteSpace(token))
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

      var identity = BuildIdentityFromJwt(token);
      return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserAuthentication(string token)
    {
      var identity = BuildIdentityFromJwt(token);
      var user = new ClaimsPrincipal(identity);
      NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
      var user = new ClaimsPrincipal(new ClaimsIdentity());
      NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    private static ClaimsIdentity BuildIdentityFromJwt(string jwt)
    {
      var handler = new JwtSecurityTokenHandler();
      var token = handler.ReadJwtToken(jwt);

      // Mapea claims /roles
      var claims = new List<Claim>(token.Claims);

      // Asegura que el rol esté bajo ClaimTypes.Role si viene en "role" o "roles"
      if (token.Claims.FirstOrDefault(c => c.Type == "role") is Claim rawRole)
        claims.Add(new Claim(ClaimTypes.Role, rawRole.Value));
      if (token.Claims.FirstOrDefault(c => c.Type == "roles") is Claim rawRoles)
        claims.Add(new Claim(ClaimTypes.Role, rawRoles.Value));

      return new ClaimsIdentity(claims, authenticationType: "jwt");
    }
  }
}
