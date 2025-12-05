// Ubicación: /Clients/Services/ClienteApiService.cs
using FacturacionElectronica.Client.DTOs; // Asegúrate que el namespace de tus DTOs sea correcto
using System.Net.Http.Json;

namespace FacturacionElectronica.Clients.Services
{
  public class ClienteApiService
  {
    private readonly HttpClient _httpClient;

    public ClienteApiService(HttpClient httpClient)
    {
      _httpClient = httpClient;
    }

    /// <summary>
    /// Obtiene la lista completa de clientes desde la API.
    /// </summary>
    /// <returns>Una lista de objetos ClienteDto.</returns>
    public async Task<List<ClienteDto>> GetClientesAsync()
    {
      // El endpoint GET /api/clientes devuelve directamente una lista.
      var clientes = await _httpClient.GetFromJsonAsync<List<ClienteDto>>("api/clientes");
      return clientes ?? new List<ClienteDto>();
    }

    /// <summary>
    /// Obtiene los detalles de un cliente específico por su cédula.
    /// </summary>
    /// <param name="cedula">La cédula del cliente a buscar.</param>
    /// <returns>El ClienteDetailDto del cliente encontrado.</returns>
    public async Task<ClienteDetailDto> GetClienteByCedulaAsync(string cedula)
    {
      var cliente = await _httpClient.GetFromJsonAsync<ClienteDetailDto>($"api/clientes/{cedula}");
      return cliente ?? throw new ApplicationException("No se pudo encontrar el cliente o la API devolvió una respuesta nula.");
    }

    /// <summary>
    /// Crea un nuevo cliente en la base de datos.
    /// </summary>
    /// <param name="newCliente">El DTO con los datos del nuevo cliente.</param>
    /// <returns>El ClienteDetailDto del cliente recién creado por la API.</returns>
    public async Task<ClienteDetailDto> CreateClienteAsync(ClienteCreateDto newCliente)
    {
      var response = await _httpClient.PostAsJsonAsync("api/clientes", newCliente);

      if (!response.IsSuccessStatusCode)
      {
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Error al crear el cliente: {errorContent}");
      }

      // Deserializa el cuerpo de la respuesta para obtener el cliente creado (la API devuelve 201 Created con el objeto)
      return await response.Content.ReadFromJsonAsync<ClienteDetailDto>()
          ?? throw new ApplicationException("La API no devolvió un cliente válido tras la creación.");
    }

    /// <summary>
    /// Actualiza un cliente existente.
    /// </summary>
    /// <param name="cedula">La cédula del cliente a modificar.</param>
    /// <param name="clienteToUpdate">El DTO con los datos actualizados del cliente.</param>
    public async Task UpdateClienteAsync(string cedula, ClienteUpdateDto clienteToUpdate)
    {
      var response = await _httpClient.PutAsJsonAsync($"api/clientes/{cedula}", clienteToUpdate);

      if (!response.IsSuccessStatusCode)
      {
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Error al actualizar el cliente: {errorContent}");
      }
    }

    /// <summary>
    /// Elimina un cliente de la base de datos.
    /// </summary>
    /// <param name="cedula">La cédula del cliente a eliminar.</param>
    public async Task DeleteClienteAsync(string cedula)
    {
      var response = await _httpClient.DeleteAsync($"api/clientes/{cedula}");

      if (!response.IsSuccessStatusCode)
      {
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new ApplicationException($"Error al eliminar el cliente: {errorContent}");
      }
    }
  }
}
