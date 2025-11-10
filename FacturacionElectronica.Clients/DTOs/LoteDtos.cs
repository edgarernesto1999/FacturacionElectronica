using System.Text.Json.Serialization;
using System.Collections.Generic;
using System; // Necesario para DateOnly

namespace FacturacionElectronica.Clients.DTOs
{
    // --- DTOs para ENVIAR datos a la API ---

    // Este DTO se usa para CREAR un nuevo lote. Está bien como lo tenías.
    public record LoteCreateDto(
        int ProductId,
        DateOnly FechaCompra,
        DateOnly? FechaExpiracion,
        decimal CostoUnitario,
        decimal PrecioVentaUnitario,
        int CantidadComprada
    );

    // --- DTOs para RECIBIR datos de la API ---

    // DTO para un lote individual dentro de la lista de un producto.
    public class LoteItemDto
    {
        [JsonPropertyName("loteId")] // <-- CORRECCIÓN: Atributo para mapear el JSON
        public int LoteId { get; set; }

        [JsonPropertyName("fechaCompra")] // <-- CORRECCIÓN
        public DateOnly FechaCompra { get; set; }

        [JsonPropertyName("fechaExpiracion")] // <-- CORRECCIÓN
        public DateOnly? FechaExpiracion { get; set; }

        [JsonPropertyName("costoUnitario")] // <-- CORRECCIÓN
        public decimal CostoUnitario { get; set; }

        [JsonPropertyName("precioVentaUnitario")] // <-- CORRECCIÓN
        public decimal PrecioVentaUnitario { get; set; }

        [JsonPropertyName("cantidadComprada")] // <-- CORRECCIÓN
        public int CantidadComprada { get; set; }

        [JsonPropertyName("cantidadDisponible")] // <-- CORRECCIÓN
        public int CantidadDisponible { get; set; }
    }

    // DTO para un producto que incluye su lista de lotes.
    public class ProductWithLotesDto
    {
        [JsonPropertyName("productoId")] // <-- CORRECCIÓN
        public int ProductoId { get; set; }

        [JsonPropertyName("tipoProducto")] // <-- CORRECCIÓN
        public string TipoProducto { get; set; }

        [JsonPropertyName("nombre")] // <-- CORRECCIÓN
        public string Nombre { get; set; }

        [JsonPropertyName("activo")] // <-- CORRECCIÓN
        public bool Activo { get; set; }

        [JsonPropertyName("lotes")] // <-- CORRECCIÓN
        public List<LoteItemDto> Lotes { get; set; } = new(); // Importante inicializar la lista
    }

    // DTO genérico para envolver la respuesta de la API. Es crucial tenerlo.
    
    
}
