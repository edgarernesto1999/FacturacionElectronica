// Ubicación: FacturacionElectronica.Clients/Program.cs
using FacturacionElectronica.Clients.Auth; // Asegúrate de que este using esté presente
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using FacturacionElectronica.Clients; // <-- AÑADE ESTA LÍNEA

public class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.RootComponents.Add<App>("#app");

    // Configura el HttpClient para que apunte a tu API
    builder.Services.AddScoped(sp => new HttpClient
    {
      BaseAddress = new Uri("https://localhost:7142") // <-- ¡IMPORTANTE! Cambia esto a la URL de tu API
    });

    // --- CONFIGURACIÓN DE AUTENTICACIÓN ---
    builder.Services.AddAuthorizationCore(); // Habilita el uso de [Authorize]

    // Registra el proveedor de estado de autenticación personalizado
    builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

    // Registra el servicio de lógica de autenticación
    builder.Services.AddScoped<AuthService>();
    // --- FIN DE LA CONFIGURACIÓN ---

    await builder.Build().RunAsync();
  }
}
