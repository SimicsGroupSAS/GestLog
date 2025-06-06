using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Models.Configuration;

namespace GestLog.ViewModels.Configuration.Logging;

/// <summary>
/// ViewModel para la configuración de logging de la aplicación
/// </summary>
public partial class LoggingConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private LoggingSettings _configuration = new();

    public LoggingConfigViewModel()
    {
        // Inicialización del ViewModel de configuración de logging
    }

    public LoggingConfigViewModel(LoggingSettings configuration)
    {
        Configuration = configuration;
    }
}
