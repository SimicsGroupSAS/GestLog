using GestLog.Models.Configuration;

namespace GestLog.Services.Configuration;

/// <summary>
/// Interfaz del servicio de configuración unificado
/// Proporciona acceso centralizado a todas las configuraciones de la aplicación
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Configuración actual de la aplicación
    /// </summary>
    AppConfiguration Current { get; }

    /// <summary>
    /// Indica si hay cambios pendientes de guardar
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// Carga la configuración desde el archivo de configuración
    /// </summary>
    /// <returns>Task que representa la operación asíncrona</returns>
    Task LoadAsync();

    /// <summary>
    /// Guarda la configuración actual al archivo
    /// </summary>
    /// <returns>Task que representa la operación asíncrona</returns>
    Task SaveAsync();

    /// <summary>
    /// Obtiene un valor de configuración específico por ruta
    /// </summary>
    /// <typeparam name="T">Tipo del valor esperado</typeparam>
    /// <param name="path">Ruta de la configuración (ej: "UI.Theme")</param>
    /// <returns>Valor de la configuración o valor por defecto</returns>
    T? GetValue<T>(string path);

    /// <summary>
    /// Establece un valor de configuración específico por ruta
    /// </summary>
    /// <typeparam name="T">Tipo del valor</typeparam>
    /// <param name="path">Ruta de la configuración (ej: "UI.Theme")</param>
    /// <param name="value">Nuevo valor</param>
    /// <returns>True si el valor se estableció correctamente</returns>
    bool SetValue<T>(string path, T value);

    /// <summary>
    /// Restaura la configuración a valores por defecto
    /// </summary>
    /// <param name="section">Sección específica a restaurar (null para toda la configuración)</param>
    Task ResetToDefaultsAsync(string? section = null);

    /// <summary>
    /// Valida la configuración actual
    /// </summary>
    /// <returns>Lista de errores de validación (vacía si es válida)</returns>
    Task<IEnumerable<string>> ValidateAsync();

    /// <summary>
    /// Exporta la configuración a un archivo
    /// </summary>
    /// <param name="filePath">Ruta del archivo de destino</param>
    Task ExportAsync(string filePath);

    /// <summary>
    /// Importa configuración desde un archivo
    /// </summary>
    /// <param name="filePath">Ruta del archivo de origen</param>
    Task ImportAsync(string filePath);

    /// <summary>
    /// Evento disparado cuando la configuración cambia
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Evento disparado antes de validar un valor
    /// </summary>
    event EventHandler<ConfigurationValidationEventArgs>? ConfigurationValidating;

    /// <summary>
    /// Evento disparado cuando se guarda la configuración
    /// </summary>
    event EventHandler<ConfigurationSavedEventArgs>? ConfigurationSaved;
}
