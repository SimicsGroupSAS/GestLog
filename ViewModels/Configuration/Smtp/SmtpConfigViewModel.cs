using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Models.Configuration;

namespace GestLog.ViewModels.Configuration.Smtp;

/// <summary>
/// ViewModel para la configuración SMTP de la aplicación
/// </summary>
public partial class SmtpConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private SmtpSettings _configuration = new();

    public SmtpConfigViewModel()
    {
        // Inicialización del ViewModel de configuración SMTP
    }

    public SmtpConfigViewModel(SmtpSettings configuration)
    {
        Configuration = configuration;
    }
}
