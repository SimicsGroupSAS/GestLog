using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Models.Configuration;

namespace GestLog.ViewModels.Configuration.UI;

/// <summary>
/// ViewModel para la configuración de interfaz de usuario de la aplicación
/// </summary>
public partial class UIConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private UISettings _configuration = new();

    public UIConfigViewModel()
    {
        // Inicialización del ViewModel de configuración de UI
    }

    public UIConfigViewModel(UISettings configuration)
    {
        Configuration = configuration;
    }
}
