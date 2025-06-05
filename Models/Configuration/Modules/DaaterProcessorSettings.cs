using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration.Modules;

/// <summary>
/// Configuraciones específicas del módulo DaaterProcessor
/// </summary>
public class DaaterProcessorSettings : INotifyPropertyChanged
{
    private string _defaultInputPath = "";
    private string _defaultOutputPath = "Output";
    private bool _validateDataOnImport = true;
    private bool _createBackupBeforeProcessing = true;
    private string _backupDirectory = "Backups";
    private bool _enableDataConsolidation = true;
    private bool _enableProviderNormalization = true;
    private bool _enableCountryMapping = true;
    private int _maxRowsPerFile = 1000000; // 1M filas
    private bool _includeHeaderRow = true;
    private string _dateFormat = "yyyy-MM-dd";
    private string _decimalSeparator = ".";
    private string _thousandsSeparator = ",";
    private bool _enableProgressReporting = true;
    private bool _enableErrorRecovery = true;

    /// <summary>
    /// Ruta de entrada por defecto para archivos a procesar
    /// </summary>
    public string DefaultInputPath
    {
        get => _defaultInputPath;
        set => SetProperty(ref _defaultInputPath, value);
    }

    /// <summary>
    /// Ruta de salida por defecto para archivos procesados
    /// </summary>
    public string DefaultOutputPath
    {
        get => _defaultOutputPath;
        set => SetProperty(ref _defaultOutputPath, value);
    }

    /// <summary>
    /// Validar datos durante la importación
    /// </summary>
    public bool ValidateDataOnImport
    {
        get => _validateDataOnImport;
        set => SetProperty(ref _validateDataOnImport, value);
    }

    /// <summary>
    /// Crear backup antes de procesar archivos
    /// </summary>
    public bool CreateBackupBeforeProcessing
    {
        get => _createBackupBeforeProcessing;
        set => SetProperty(ref _createBackupBeforeProcessing, value);
    }

    /// <summary>
    /// Directorio para archivos de backup
    /// </summary>
    public string BackupDirectory
    {
        get => _backupDirectory;
        set => SetProperty(ref _backupDirectory, value);
    }

    /// <summary>
    /// Habilitar consolidación de datos
    /// </summary>
    public bool EnableDataConsolidation
    {
        get => _enableDataConsolidation;
        set => SetProperty(ref _enableDataConsolidation, value);
    }

    /// <summary>
    /// Habilitar normalización de nombres de proveedores
    /// </summary>
    public bool EnableProviderNormalization
    {
        get => _enableProviderNormalization;
        set => SetProperty(ref _enableProviderNormalization, value);
    }

    /// <summary>
    /// Habilitar mapeo de países
    /// </summary>
    public bool EnableCountryMapping
    {
        get => _enableCountryMapping;
        set => SetProperty(ref _enableCountryMapping, value);
    }

    /// <summary>
    /// Número máximo de filas por archivo de salida
    /// </summary>
    public int MaxRowsPerFile
    {
        get => _maxRowsPerFile;
        set => SetProperty(ref _maxRowsPerFile, Math.Max(1000, value));
    }

    /// <summary>
    /// Incluir fila de encabezados en archivos de salida
    /// </summary>
    public bool IncludeHeaderRow
    {
        get => _includeHeaderRow;
        set => SetProperty(ref _includeHeaderRow, value);
    }

    /// <summary>
    /// Formato de fecha para exportación
    /// </summary>
    public string DateFormat
    {
        get => _dateFormat;
        set => SetProperty(ref _dateFormat, value);
    }

    /// <summary>
    /// Separador decimal
    /// </summary>
    public string DecimalSeparator
    {
        get => _decimalSeparator;
        set => SetProperty(ref _decimalSeparator, value);
    }

    /// <summary>
    /// Separador de miles
    /// </summary>
    public string ThousandsSeparator
    {
        get => _thousandsSeparator;
        set => SetProperty(ref _thousandsSeparator, value);
    }

    /// <summary>
    /// Habilitar reportes de progreso detallados
    /// </summary>
    public bool EnableProgressReporting
    {
        get => _enableProgressReporting;
        set => SetProperty(ref _enableProgressReporting, value);
    }

    /// <summary>
    /// Habilitar recuperación automática de errores
    /// </summary>
    public bool EnableErrorRecovery
    {
        get => _enableErrorRecovery;
        set => SetProperty(ref _enableErrorRecovery, value);
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
