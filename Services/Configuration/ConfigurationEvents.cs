using GestLog.Models.Configuration;

namespace GestLog.Services.Configuration;

/// <summary>
/// Eventos relacionados con cambios de configuración
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public string SettingPath { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    public DateTime Timestamp { get; }

    public ConfigurationChangedEventArgs(string settingPath, object? oldValue, object? newValue)
    {
        SettingPath = settingPath;
        OldValue = oldValue;
        NewValue = newValue;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// Eventos de validación de configuración
/// </summary>
public class ConfigurationValidationEventArgs : EventArgs
{
    public string SettingPath { get; }
    public object? Value { get; }
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }

    public ConfigurationValidationEventArgs(string settingPath, object? value)
    {
        SettingPath = settingPath;
        Value = value;
    }
}

/// <summary>
/// Eventos de guardado de configuración
/// </summary>
public class ConfigurationSavedEventArgs : EventArgs
{
    public AppConfiguration Configuration { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public DateTime Timestamp { get; }

    public ConfigurationSavedEventArgs(AppConfiguration configuration, bool success, string? errorMessage = null)
    {
        Configuration = configuration;
        Success = success;
        ErrorMessage = errorMessage;
        Timestamp = DateTime.Now;
    }
}
