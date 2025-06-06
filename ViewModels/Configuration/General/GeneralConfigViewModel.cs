using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Models.Configuration;

namespace GestLog.ViewModels.Configuration.General;

/// <summary>
/// ViewModel para la configuración general de la aplicación
/// </summary>
public partial class GeneralConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private GeneralSettings _configuration = new();

    public GeneralConfigViewModel()
    {
        // Inicialización del ViewModel de configuración general
    }

    public GeneralConfigViewModel(GeneralSettings configuration)
    {
        Configuration = configuration;
    }
}
