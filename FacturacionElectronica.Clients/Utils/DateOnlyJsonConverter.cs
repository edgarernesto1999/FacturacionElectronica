using System.Text.Json;
using System.Text.Json.Serialization;

// El namespace debe coincidir con la ubicación del archivo: NombreDelProyecto.NombreDeLaCarpeta
namespace FacturacionElectronica.Clients.Utils
{
  /// <summary>
  /// Convertidor personalizado para que System.Text.Json sepa cómo manejar
  /// el tipo de dato DateOnly. Se encarga de serializar (convertir a texto)
  /// y deserializar (convertir desde texto) usando el formato estándar "yyyy-MM-dd".
  /// </summary>
  public class DateOnlyJsonConverter : JsonConverter<DateOnly>
  {
    // Definimos el formato de fecha estándar que usará la API (ISO 8601 para fecha).
    private const string Format = "yyyy-MM-dd";

    /// <summary>
    /// Define cómo escribir un objeto DateOnly en el flujo JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
      // Convierte el DateOnly a un string con el formato "yyyy-MM-dd" y lo escribe.
      writer.WriteStringValue(value.ToString(Format));
    }

    /// <summary>
    /// Define cómo leer una cadena de texto desde el flujo JSON y convertirla a un objeto DateOnly.
    /// </summary>
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      // Lee el string del JSON y lo convierte a DateOnly usando el formato exacto.
      // El '!' al final de GetString() indica que confiamos en que el valor no será nulo.
      return DateOnly.ParseExact(reader.GetString()!, Format);
    }
  }
}
