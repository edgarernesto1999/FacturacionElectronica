// Ubicación: /Clients/Services/UsuarioApiService.cs

using System.Net.Http.Json;
using FacturacionElectronica.Clients.DTOs;

// El namespace se mantiene igual
namespace FacturacionElectronica.Clients.Services
{
  public class UsuarioApiService
  {
    private readonly HttpClient _httpClient;

    public UsuarioApiService(HttpClient httpClient)
    {
      _httpClient = httpClient;
    }

    // ==================================================================
    // === INICIO DE LA MODIFICACIÓN ===
    // ==================================================================

    /// <summary>
    /// Obtiene la lista de usuarios desde la API.
    /// Ahora espera un objeto contenedor (ApiResponseDto) y extrae la lista de datos.
    /// </summary>
    /// <returns>Una lista de UserDto o una lista vacía si no hay datos.</returns>
    public async Task<List<UserDto>> GetUsuariosAsync()
    {
      // 1. Le pedimos a HttpClient que deserialice la respuesta usando nuestra nueva clase contenedora.
      //    Ahora entiende la estructura { "total": ..., "data": [...] }.
      var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponseDto<UserDto>>("api/usuarios");

      // 2. Devolvemos únicamente la propiedad 'Data' que contiene la lista de usuarios.
      //    Si la respuesta o la propiedad 'Data' son nulas, devolvemos una lista vacía
      //    para evitar errores en el componente Razor.
      return apiResponse?.Data ?? new List<UserDto>();
    }

    // ==================================================================
    // === FIN DE LA MODIFICACIÓN ===
    // ==================================================================


    /// <summary>
    /// Crea un nuevo usuario.
    /// (Este método no necesita cambios)
    /// </summary>
    public async Task CreateUsuarioAsync(CreateUserDto newUser)
    {
      var response = await _httpClient.PostAsJsonAsync("api/usuarios", newUser);
      response.EnsureSuccessStatusCode(); // Lanza una excepción si la API devuelve un error (4xx o 5xx)
    }

    /// <summary>
    /// Actualiza el estado de un usuario existente.
    /// (Este método no necesita cambios)
    /// </summary>
    public async Task UpdateUsuarioEstadoAsync(int userId, string nuevoEstado)
    {
      // La ruta debe coincidir con la definida en tu API
      var response = await _httpClient.PutAsJsonAsync($"api/usuarios/{userId}/estado", new { Estado = nuevoEstado });
      response.EnsureSuccessStatusCode();
    }
  }
}
