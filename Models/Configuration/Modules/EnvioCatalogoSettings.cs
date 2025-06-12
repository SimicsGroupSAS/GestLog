using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration.Modules;

/// <summary>
/// Configuraciones específicas del módulo de Envío de Catálogo
/// </summary>
public class EnvioCatalogoSettings : INotifyPropertyChanged
{
    private SmtpSettings _smtp = new();

    /// <summary>
    /// Configuración SMTP específica para el módulo de Envío de Catálogo
    /// </summary>
    public SmtpSettings Smtp
    {
        get => _smtp;
        set => SetProperty(ref _smtp, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
