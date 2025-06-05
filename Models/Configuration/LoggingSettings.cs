using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones del sistema de logging
/// </summary>
public class LoggingSettings : INotifyPropertyChanged
{
    private LogLevel _minimumLevel = LogLevel.Information;
    private bool _enableFileLogging = true;
    private bool _enableConsoleLogging = true;
    private string _logDirectory = "Logs";
    private int _maxLogFiles = 30;
    private long _maxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
    private string _logFilePattern = "gestlog-{Date}.txt";
    private bool _structuredLogging = true;
    private bool _includeSourceContext = true;
    private bool _enablePerformanceMetrics = true;
    private bool _logUserInteractions = true;
    private bool _logExceptionDetails = true;

    /// <summary>
    /// Nivel mínimo de logging
    /// </summary>
    public LogLevel MinimumLevel
    {
        get => _minimumLevel;
        set => SetProperty(ref _minimumLevel, value);
    }

    /// <summary>
    /// Habilitar logging a archivos
    /// </summary>
    public bool EnableFileLogging
    {
        get => _enableFileLogging;
        set => SetProperty(ref _enableFileLogging, value);
    }

    /// <summary>
    /// Habilitar logging a consola
    /// </summary>
    public bool EnableConsoleLogging
    {
        get => _enableConsoleLogging;
        set => SetProperty(ref _enableConsoleLogging, value);
    }

    /// <summary>
    /// Directorio donde se guardan los logs
    /// </summary>
    public string LogDirectory
    {
        get => _logDirectory;
        set => SetProperty(ref _logDirectory, value);
    }

    /// <summary>
    /// Número máximo de archivos de log a mantener
    /// </summary>
    public int MaxLogFiles
    {
        get => _maxLogFiles;
        set => SetProperty(ref _maxLogFiles, Math.Max(1, value));
    }

    /// <summary>
    /// Tamaño máximo por archivo de log en bytes
    /// </summary>
    public long MaxFileSizeBytes
    {
        get => _maxFileSizeBytes;
        set => SetProperty(ref _maxFileSizeBytes, Math.Max(1024 * 1024, value)); // Mínimo 1MB
    }

    /// <summary>
    /// Patrón de nombre para archivos de log
    /// </summary>
    public string LogFilePattern
    {
        get => _logFilePattern;
        set => SetProperty(ref _logFilePattern, value);
    }

    /// <summary>
    /// Usar logging estructurado (JSON)
    /// </summary>
    public bool StructuredLogging
    {
        get => _structuredLogging;
        set => SetProperty(ref _structuredLogging, value);
    }

    /// <summary>
    /// Incluir contexto de origen en los logs
    /// </summary>
    public bool IncludeSourceContext
    {
        get => _includeSourceContext;
        set => SetProperty(ref _includeSourceContext, value);
    }

    /// <summary>
    /// Habilitar métricas de rendimiento
    /// </summary>
    public bool EnablePerformanceMetrics
    {
        get => _enablePerformanceMetrics;
        set => SetProperty(ref _enablePerformanceMetrics, value);
    }

    /// <summary>
    /// Registrar interacciones del usuario
    /// </summary>
    public bool LogUserInteractions
    {
        get => _logUserInteractions;
        set => SetProperty(ref _logUserInteractions, value);
    }

    /// <summary>
    /// Incluir detalles completos de excepciones
    /// </summary>
    public bool LogExceptionDetails
    {
        get => _logExceptionDetails;
        set => SetProperty(ref _logExceptionDetails, value);
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
