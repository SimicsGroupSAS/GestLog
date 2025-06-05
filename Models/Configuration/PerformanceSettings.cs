using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones de rendimiento y optimización
/// </summary>
public class PerformanceSettings : INotifyPropertyChanged
{
    private int _maxConcurrentOperations = Environment.ProcessorCount;
    private int _progressUpdateInterval = 100; // milisegundos
    private bool _enableCaching = true;
    private long _maxMemoryUsageBytes = 1024 * 1024 * 1024; // 1GB
    private int _fileProcessingBatchSize = 10;
    private bool _enableProgressSmoothing = true;
    private double _progressSmoothingFactor = 0.3;
    private bool _enableFilePreloading = true;
    private int _maxRecentItemsCache = 100;
    private bool _enableGarbageCollectionOptimization = true;

    /// <summary>
    /// Número máximo de operaciones concurrentes
    /// </summary>
    public int MaxConcurrentOperations
    {
        get => _maxConcurrentOperations;
        set => SetProperty(ref _maxConcurrentOperations, Math.Max(1, Math.Min(Environment.ProcessorCount * 2, value)));
    }

    /// <summary>
    /// Intervalo de actualización del progreso en milisegundos
    /// </summary>
    public int ProgressUpdateInterval
    {
        get => _progressUpdateInterval;
        set => SetProperty(ref _progressUpdateInterval, Math.Max(50, Math.Min(1000, value)));
    }

    /// <summary>
    /// Habilitar cache de datos en memoria
    /// </summary>
    public bool EnableCaching
    {
        get => _enableCaching;
        set => SetProperty(ref _enableCaching, value);
    }

    /// <summary>
    /// Límite máximo de uso de memoria en bytes
    /// </summary>
    public long MaxMemoryUsageBytes
    {
        get => _maxMemoryUsageBytes;
        set => SetProperty(ref _maxMemoryUsageBytes, Math.Max(256 * 1024 * 1024, value)); // Mínimo 256MB
    }

    /// <summary>
    /// Tamaño de lote para procesamiento de archivos
    /// </summary>
    public int FileProcessingBatchSize
    {
        get => _fileProcessingBatchSize;
        set => SetProperty(ref _fileProcessingBatchSize, Math.Max(1, Math.Min(100, value)));
    }

    /// <summary>
    /// Habilitar suavizado de animaciones de progreso
    /// </summary>
    public bool EnableProgressSmoothing
    {
        get => _enableProgressSmoothing;
        set => SetProperty(ref _enableProgressSmoothing, value);
    }

    /// <summary>
    /// Factor de suavizado para animaciones (0.0 - 1.0)
    /// </summary>
    public double ProgressSmoothingFactor
    {
        get => _progressSmoothingFactor;
        set => SetProperty(ref _progressSmoothingFactor, Math.Max(0.1, Math.Min(1.0, value)));
    }

    /// <summary>
    /// Habilitar precarga de archivos para mejorar rendimiento
    /// </summary>
    public bool EnableFilePreloading
    {
        get => _enableFilePreloading;
        set => SetProperty(ref _enableFilePreloading, value);
    }

    /// <summary>
    /// Número máximo de elementos en cache de items recientes
    /// </summary>
    public int MaxRecentItemsCache
    {
        get => _maxRecentItemsCache;
        set => SetProperty(ref _maxRecentItemsCache, Math.Max(10, Math.Min(1000, value)));
    }

    /// <summary>
    /// Habilitar optimizaciones de garbage collection
    /// </summary>
    public bool EnableGarbageCollectionOptimization
    {
        get => _enableGarbageCollectionOptimization;
        set => SetProperty(ref _enableGarbageCollectionOptimization, value);
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
