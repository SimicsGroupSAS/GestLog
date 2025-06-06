using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Models.Configuration;

namespace GestLog.ViewModels.Configuration.Performance;

/// <summary>
/// ViewModel para la configuración de rendimiento de la aplicación
/// </summary>
public partial class PerformanceConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private PerformanceSettings _configuration = new();

    public PerformanceConfigViewModel()
    {
        // Inicialización del ViewModel de configuración de rendimiento
    }

    public PerformanceConfigViewModel(PerformanceSettings configuration)
    {
        Configuration = configuration;
    }
}
