// Ubicación: /Clients/Services/UsuarioApiService.cs

using FacturacionElectronica.Clients.DTOs;
using System.Net.Http.Json;
using static FacturacionElectronica.Clients.Pages.Admin.AdministrarUser;

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

    /// <summary>
    /// Obtiene la lista de usuarios desde la API.
    /// (Este método ya era correcto y no se ha modificado).
    /// </summary>
    public async Task<List<UserDto>> GetUsuariosAsync()
    {
      var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponseDto<UserDto>>("api/usuarios");
      return apiResponse?.Data ?? new List<UserDto>();
    }

    // ==================================================================
    // === INICIO DE LAS MODIFICACIONES ===
    // ==================================================================

    /// <summary>
    /// Crea un nuevo usuario y devuelve el objeto del usuario creado.
    /// </summary>
    /// <param name="newUser">El DTO con los datos del nuevo usuario.</param>
    /// <returns>El UserDto del usuario recién creado por la API.</returns>
    public async Task<UserDto> CreateUserAsync(CreateUserDto newUser)
    {
      var response = await _httpClient.PostAsJsonAsync("api/usuarios", newUser);

      if (!response.IsSuccessStatusCode)
      {
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Error al crear el usuario: {errorContent}");
      }

      // Deserializa el cuerpo de la respuesta para obtener el usuario creado.
      return await response.Content.ReadFromJsonAsync<UserDto>()
             ?? throw new ApplicationException("La API no devolvió un usuario válido tras la creación.");
    }

    /// <summary>
    /// Actualiza el estado de un usuario existente usando el método PATCH.
    /// </summary>
    /// <param name="userId">El ID del usuario a modificar.</param>
    /// <param name="dto">El DTO que contiene el nuevo estado.</param>
    public async Task UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto)
    {
      // Se usa PATCH, que es más adecuado para actualizaciones parciales (solo un campo).
      // La ruta /estado debe coincidir con tu endpoint en el backend.
      var response = await _httpClient.PatchAsJsonAsync($"api/usuarios/{userId}/estado", dto);

      if (!response.IsSuccessStatusCode)
      {
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Error al actualizar el estado del usuario: {errorContent}");
      }
    }
  }
}
