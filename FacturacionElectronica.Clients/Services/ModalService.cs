using System;

namespace FacturacionElectronica.Clients.Services // Asegúrate que el namespace sea correcto
{
  public class ModalService
  {
    public bool IsVisible { get; private set; }
    public string Title { get; private set; } = "";
    public string Message { get; private set; } = "";

    // Evento que se disparará cuando el estado del modal cambie
    public event Action? OnChange;

    public void Show(string title, string message)
    {
      IsVisible = true;
      Title = title;
      Message = message;
      NotifyStateChanged();
    }

    public void Hide()
    {
      IsVisible = false;
      NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
  }
}
