namespace FacturacionElectronica.Api.DTOs
{
  public record ProductCreateDto(
    string TipoProducto,
    String Nombre,
    bool Activo = true
    );

  public record ProductUpdateDto(
    string TipoProducto,
    string Nombre,
    bool Activo
    );

  public record ProductListItemDto(
    int ProductId,
    string TipoProducto,
    string Nombre,
    bool Activo,
    int stock
    );

}
