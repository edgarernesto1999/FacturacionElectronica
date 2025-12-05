namespace FacturacionElectronica.Api.DTOs
{
  //DTO para crear un nuevo producto
  public record ProductCreateDto(
    string TipoProducto,
    String Nombre,
    String Marca,
    String Presentacion,
    bool Activo = true

    );

//DTO para actualizar un producto existente
  public record ProductUpdateDto(
    string TipoProducto,
    string Nombre,
    bool Activo,
    String Marca,
    String Presentacion
    );

//DTO para mostrar los productos
  public record ProductListItemDto(
    int ProductId,
    string TipoProducto,
    string Nombre,
    bool Activo,
    int stock,
    String Marca,
    String Presentacion
    );

}
