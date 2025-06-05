using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones de módulos específicos de la aplicación
/// </summary>
public class ModulesConfiguration : INotifyPropertyChanged
{
    private Modules.DaaterProcessorSettings _daaterProcessor = new();
    private Modules.ErrorLogSettings _errorLog = new();

    /// <summary>
    /// Configuraciones del módulo DaaterProcessor
    /// </summary>
    public Modules.DaaterProcessorSettings DaaterProcessor
    {
        get => _daaterProcessor;
        set => SetProperty(ref _daaterProcessor, value);
    }

    /// <summary>
    /// Configuraciones del registro de errores
    /// </summary>
    public Modules.ErrorLogSettings ErrorLog
    {
        get => _errorLog;
        set => SetProperty(ref _errorLog, value);
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
