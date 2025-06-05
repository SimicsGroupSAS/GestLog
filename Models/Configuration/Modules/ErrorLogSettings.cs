using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration.Modules;

/// <summary>
/// Configuraciones específicas del registro de errores
/// </summary>
public class ErrorLogSettings : INotifyPropertyChanged
{
    private int _maxErrorsToStore = 1000;
    private bool _autoRefresh = true;
    private int _autoRefreshInterval = 5000; // milisegundos
    private bool _showErrorDetails = true;
    private bool _enableErrorNotifications = true;
    private bool _groupSimilarErrors = true;
    private bool _enableErrorExport = true;
    private string _exportFormat = "JSON";
    private bool _enableErrorFiltering = true;
    private bool _showTimestamps = true;
    private bool _showContextInformation = true;
    private bool _enableStackTraceView = true;
    private int _errorRetentionDays = 30;

    /// <summary>
    /// Número máximo de errores a almacenar en memoria
    /// </summary>
    public int MaxErrorsToStore
    {
        get => _maxErrorsToStore;
        set => SetProperty(ref _maxErrorsToStore, Math.Max(100, Math.Min(10000, value)));
    }

    /// <summary>
    /// Actualizar automáticamente la vista de errores
    /// </summary>
    public bool AutoRefresh
    {
        get => _autoRefresh;
        set => SetProperty(ref _autoRefresh, value);
    }

    /// <summary>
    /// Intervalo de actualización automática en milisegundos
    /// </summary>
    public int AutoRefreshInterval
    {
        get => _autoRefreshInterval;
        set => SetProperty(ref _autoRefreshInterval, Math.Max(1000, Math.Min(60000, value)));
    }

    /// <summary>
    /// Mostrar detalles completos de errores
    /// </summary>
    public bool ShowErrorDetails
    {
        get => _showErrorDetails;
        set => SetProperty(ref _showErrorDetails, value);
    }

    /// <summary>
    /// Habilitar notificaciones de errores
    /// </summary>
    public bool EnableErrorNotifications
    {
        get => _enableErrorNotifications;
        set => SetProperty(ref _enableErrorNotifications, value);
    }

    /// <summary>
    /// Agrupar errores similares para reducir ruido
    /// </summary>
    public bool GroupSimilarErrors
    {
        get => _groupSimilarErrors;
        set => SetProperty(ref _groupSimilarErrors, value);
    }

    /// <summary>
    /// Habilitar exportación de logs de errores
    /// </summary>
    public bool EnableErrorExport
    {
        get => _enableErrorExport;
        set => SetProperty(ref _enableErrorExport, value);
    }

    /// <summary>
    /// Formato de exportación (JSON, CSV, XML)
    /// </summary>
    public string ExportFormat
    {
        get => _exportFormat;
        set => SetProperty(ref _exportFormat, value);
    }

    /// <summary>
    /// Habilitar filtrado de errores por tipo/severidad
    /// </summary>
    public bool EnableErrorFiltering
    {
        get => _enableErrorFiltering;
        set => SetProperty(ref _enableErrorFiltering, value);
    }

    /// <summary>
    /// Mostrar timestamps de errores
    /// </summary>
    public bool ShowTimestamps
    {
        get => _showTimestamps;
        set => SetProperty(ref _showTimestamps, value);
    }

    /// <summary>
    /// Mostrar información de contexto de errores
    /// </summary>
    public bool ShowContextInformation
    {
        get => _showContextInformation;
        set => SetProperty(ref _showContextInformation, value);
    }

    /// <summary>
    /// Habilitar vista de stack trace
    /// </summary>
    public bool EnableStackTraceView
    {
        get => _enableStackTraceView;
        set => SetProperty(ref _enableStackTraceView, value);
    }

    /// <summary>
    /// Días de retención de errores antes de eliminar automáticamente
    /// </summary>
    public int ErrorRetentionDays
    {
        get => _errorRetentionDays;
        set => SetProperty(ref _errorRetentionDays, Math.Max(1, Math.Min(365, value)));
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
