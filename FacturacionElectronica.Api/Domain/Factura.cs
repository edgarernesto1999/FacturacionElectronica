namespace FacturacionElectronica.Api.Domain
{
  /// <summary>
  /// Value Object de Cliente que se almacena dentro de la Factura
  /// como una fotografía del cliente en el momento de la emisión.
  /// No tiene identidad propia. Es inmutable.
  /// </summary>
  public class ClienteFacturaVO
  {
    public string TipoIdentificacion { get; }
    public string Identificacion { get; }
    public string NombreCompleto { get; }
    public string? Direccion { get; }

    public ClienteFacturaVO(
        string tipoIdentificacion,
        string identificacion,
        string nombreCompleto,
        string? direccion)
    {
      TipoIdentificacion = tipoIdentificacion;
      Identificacion = identificacion;
      NombreCompleto = nombreCompleto;
      Direccion = direccion;
    }

    // Sobrescribir igualdad para comportamiento real de VO
    public override bool Equals(object? obj)
    {
      if (obj is not ClienteFacturaVO other)
        return false;

      return TipoIdentificacion == other.TipoIdentificacion
          && Identificacion == other.Identificacion
          && NombreCompleto == other.NombreCompleto
          && Direccion == other.Direccion;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(TipoIdentificacion, Identificacion, NombreCompleto, Direccion);
    }
  }
}
