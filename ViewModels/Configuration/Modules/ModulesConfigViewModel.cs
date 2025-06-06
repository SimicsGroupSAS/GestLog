using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Models.Configuration;

namespace GestLog.ViewModels.Configuration.Modules;

/// <summary>
/// ViewModel para la configuración de módulos de la aplicación
/// </summary>
public partial class ModulesConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private ModulesConfiguration _configuration = new();

    public ModulesConfigViewModel()
    {
        // Inicialización del ViewModel de configuración de módulos
    }

    public ModulesConfigViewModel(ModulesConfiguration configuration)
    {
        Configuration = configuration;
    }
}
