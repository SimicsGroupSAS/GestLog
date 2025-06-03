using Microsoft.Extensions.Logging;

namespace GestLog.Services;

/// <summary>
/// Interfaz para el servicio de logging de GestLog
/// Proporciona métodos específicos del dominio para logging estructurado
/// </summary>
public interface IGestLogLogger
{
    /// <summary>
    /// Logger base de Microsoft.Extensions.Logging
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Registra el inicio de procesamiento de archivos Excel
    /// </summary>
    void LogExcelProcessingStarted(string folderPath, int fileCount);

    /// <summary>
    /// Registra el progreso de procesamiento de archivos Excel
    /// </summary>
    void LogExcelProcessingProgress(string fileName, int currentFile, int totalFiles, double percentage);

    /// <summary>
    /// Registra la finalización exitosa del procesamiento
    /// </summary>
    void LogExcelProcessingCompleted(string outputPath, TimeSpan duration, int processedFiles);

    /// <summary>
    /// Registra errores durante el procesamiento de Excel
    /// </summary>
    void LogExcelProcessingError(string fileName, Exception exception);

    /// <summary>
    /// Registra operaciones de cancelación
    /// </summary>
    void LogOperationCancelled(string operationType, string reason = "");

    /// <summary>
    /// Registra navegación entre vistas
    /// </summary>
    void LogNavigation(string fromView, string toView, string userId = "");

    /// <summary>
    /// Registra eventos de rendimiento
    /// </summary>
    void LogPerformance(string operation, TimeSpan duration, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Registra eventos de configuración
    /// </summary>
    void LogConfiguration(string setting, object value, string source = "");

    /// <summary>
    /// Registra excepciones no manejadas
    /// </summary>
    void LogUnhandledException(Exception exception, string context = "");
}
