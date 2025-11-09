namespace FacturacionElectronica.Clients.Shar
{
  // Usaremos este servicio para la comunicación entre componentes
  public class ToastService : IDisposable
  {
    // Evento que se disparará cuando se solicite mostrar un toast
    public event Action<string, ToastLevel>? OnShow;

    // Método que los componentes llamarán para mostrar un toast
    public void ShowToast(string message, ToastLevel level = ToastLevel.Success)
    {
      OnShow?.Invoke(message, level);
    }

    public void Dispose()
    {
      // Lógica de limpieza si fuera necesaria
    }
  }

  // Enum para definir los diferentes tipos de toast (éxito, error, etc.)
  public enum ToastLevel
  {
    Info,
    Success,
    Warning,
    Error
  }
}
