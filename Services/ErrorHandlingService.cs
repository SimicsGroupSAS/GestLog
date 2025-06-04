using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace GestLog.Services;

/// <summary>
/// Servicio centralizado para la gestión de errores en la aplicación GestLog
/// Proporciona manejo de excepciones unificado y registro de errores
/// </summary>
public interface IErrorHandlingService
{
    /// <summary>
    /// Ejecuta una acción con manejo de excepción
    /// </summary>
    void HandleOperation(Action operation, string operationName);

    /// <summary>
    /// Ejecuta una acción con manejo de excepción y un comportamiento personalizado en caso de error
    /// </summary>
    void HandleOperation(Action operation, string operationName, Action<Exception> onError);

    /// <summary>
    /// Ejecuta una función con manejo de excepción
    /// </summary>
    T? HandleOperation<T>(Func<T> operation, string operationName, T? defaultValue = default);

    /// <summary>
    /// Ejecuta una operación asíncrona con manejo de excepción
    /// </summary>
    Task HandleOperationAsync(Func<Task> operation, string operationName);

    /// <summary>
    /// Ejecuta una función asíncrona con manejo de excepción
    /// </summary>
    Task<T?> HandleOperationAsync<T>(Func<Task<T>> operation, string operationName, T? defaultValue = default);

    /// <summary>
    /// Maneja una excepción específica con mensaje personalizado
    /// </summary>
    void HandleException(Exception exception, string context, bool showToUser = true);

    /// <summary>
    /// Obtiene los últimos errores registrados
    /// </summary>
    ReadOnlyCollection<ErrorRecord> GetRecentErrors();
    
    /// <summary>
    /// Evento que se dispara cuando ocurre un error
    /// </summary>
    event EventHandler<ErrorEventArgs> ErrorOccurred;
}

/// <summary>
/// Implementación del servicio de manejo de errores
/// </summary>
public class ErrorHandlingService : IErrorHandlingService
{
    private readonly IGestLogLogger _logger;
    private readonly List<ErrorRecord> _recentErrors = new(capacity: 50);
    private const int MaxStoredErrors = 50;

    public event EventHandler<ErrorEventArgs>? ErrorOccurred;

    public ErrorHandlingService(IGestLogLogger logger)
    {
        _logger = logger;
    }

    public void HandleOperation(Action operation, string operationName)
    {
        try
        {
            _logger.LogDebug("🔄 Iniciando operación: {OperationName}", operationName);
            operation();
            _logger.LogDebug("✅ Operación completada correctamente: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName);
        }
    }

    public void HandleOperation(Action operation, string operationName, Action<Exception> onError)
    {
        try
        {
            _logger.LogDebug("🔄 Iniciando operación: {OperationName}", operationName);
            operation();
            _logger.LogDebug("✅ Operación completada correctamente: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName, false);
            onError(ex);
        }
    }

    public T? HandleOperation<T>(Func<T> operation, string operationName, T? defaultValue = default)
    {
        try
        {
            _logger.LogDebug("🔄 Iniciando operación: {OperationName}", operationName);
            var result = operation();
            _logger.LogDebug("✅ Operación completada correctamente: {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName);
            return defaultValue;
        }
    }

    public async Task HandleOperationAsync(Func<Task> operation, string operationName)
    {
        try
        {
            _logger.LogDebug("🔄 Iniciando operación asíncrona: {OperationName}", operationName);
            await operation();
            _logger.LogDebug("✅ Operación asíncrona completada correctamente: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName);
        }
    }

    public async Task<T?> HandleOperationAsync<T>(Func<Task<T>> operation, string operationName, T? defaultValue = default)
    {
        try
        {
            _logger.LogDebug("🔄 Iniciando operación asíncrona: {OperationName}", operationName);
            var result = await operation();
            _logger.LogDebug("✅ Operación asíncrona completada correctamente: {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            HandleException(ex, operationName);
            return defaultValue;
        }
    }

    public void HandleException(Exception exception, string context, bool showToUser = true)
    {
        // Ignorar excepciones de cancelación de operación
        if (exception is OperationCanceledException or TaskCanceledException)
        {
            _logger.LogInformation("⏹️ Operación cancelada: {Context}", context);
            return;
        }

        // Registrar el error
        var errorDate = DateTime.Now;
        var errorId = Guid.NewGuid().ToString("N").Substring(0, 8);

        // Registrar en el logger
        _logger.LogError(exception, "❌ Error en {Context} [ID: {ErrorId}]", context, errorId);

        // Crear y almacenar registro de error
        var errorRecord = new ErrorRecord
        {
            Id = errorId,
            Timestamp = errorDate,
            Context = context,
            Message = exception.Message,
            ExceptionType = exception.GetType().Name,
            StackTrace = exception.StackTrace ?? "No disponible"
        };

        // Almacenar en la lista interna con límite
        lock (_recentErrors)
        {
            _recentErrors.Add(errorRecord);
            if (_recentErrors.Count > MaxStoredErrors)
            {
                _recentErrors.RemoveAt(0);
            }
        }

        // Disparar evento de error
        OnErrorOccurred(errorRecord);

        // Mostrar al usuario si es necesario
        if (showToUser)
        {
            // Ejecutar en el hilo de UI
            if (Application.Current?.Dispatcher != null && 
                !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => 
                    ShowErrorToUser(errorRecord, exception));
            }
            else
            {
                ShowErrorToUser(errorRecord, exception);
            }
        }
    }

    private void ShowErrorToUser(ErrorRecord errorRecord, Exception ex)
    {
        var message = $"Se ha producido un error:\n\n" +
            $"{errorRecord.Message}\n\n" +
            $"Contexto: {errorRecord.Context}\n" +
            $"ID: {errorRecord.Id}\n\n" +
            "Este error ha sido registrado para su análisis.";

        MessageBox.Show(
            message,
            "Error en la aplicación",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public ReadOnlyCollection<ErrorRecord> GetRecentErrors()
    {
        lock (_recentErrors)
        {
            return new ReadOnlyCollection<ErrorRecord>(_recentErrors.ToList());
        }
    }

    protected virtual void OnErrorOccurred(ErrorRecord errorRecord)
    {
        ErrorOccurred?.Invoke(this, new ErrorEventArgs(errorRecord));
    }
}

/// <summary>
/// Registro de un error que ha ocurrido en la aplicación
/// </summary>
public class ErrorRecord
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Context { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
}

/// <summary>
/// Argumentos del evento de error
/// </summary>
public class ErrorEventArgs : EventArgs
{
    public ErrorRecord Error { get; }

    public ErrorEventArgs(ErrorRecord error)
    {
        Error = error;
    }
}
