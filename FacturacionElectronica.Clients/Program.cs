// Ubicación: FacturacionElectronica.Clients/Program.cs
using FacturacionElectronica.Clients;
using FacturacionElectronica.Clients.Auth;
using FacturacionElectronica.Clients.Services; 
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using FacturacionElectronica.Clients.Shar;

public class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.RootComponents.Add<App>("#app");

    builder.Services.AddScoped(sp => new HttpClient
    {
      BaseAddress = new Uri("https://localhost:7142")
    });
    builder.Services.AddScoped<ProductoApiService>();
    builder.Services.AddScoped<ToastService>();

    // --- CONFIGURACIÓN DE AUTENTICACIÓN (NO SE TOCA) ---
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
    builder.Services.AddScoped<AuthService>();
    // --- FIN DE LA CONFIGURACIÓN ---

    // ==================================================================
    // === AÑADE ESTA LÍNEA PARA REGISTRAR EL SERVICIO DE PRODUCTOS ===
    // ==================================================================
    builder.Services.AddScoped<ProductoApiService>();


    await builder.Build().RunAsync();
  }
}
