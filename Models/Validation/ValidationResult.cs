using System;
using System.Collections.Generic;
using System.Linq;

namespace GestLog.Models.Validation;

/// <summary>
/// Resultado de una operación de validación
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Lista de errores de validación
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Lista de advertencias de validación
    /// </summary>
    public List<ValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Indica si la validación fue exitosa (sin errores)
    /// </summary>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Indica si hay advertencias
    /// </summary>
    public bool HasWarnings => Warnings.Any();

    /// <summary>
    /// Número total de errores
    /// </summary>
    public int ErrorCount => Errors.Count;    /// <summary>
    /// Número total de advertencias
    /// </summary>
    public int WarningCount => Warnings.Count;

    /// <summary>
    /// Crea un resultado de validación exitoso
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult();
    }

    /// <summary>
    /// Crea un resultado de validación con un error específico
    /// </summary>
    public static ValidationResult WithError(string propertyName, string message)
    {
        var result = new ValidationResult();
        result.AddError(propertyName, message);
        return result;
    }

    /// <summary>
    /// Resumen del resultado de validación
    /// </summary>
    public string Summary
    {
        get
        {
            if (IsValid && !HasWarnings)
                return "✅ Validación exitosa";
            
            if (IsValid && HasWarnings)
                return $"⚠️ Validación exitosa con {WarningCount} advertencia(s)";
            
            return $"❌ Validación fallida: {ErrorCount} error(es)";
        }
    }

    /// <summary>
    /// Agrega un error de validación
    /// </summary>
    public void AddError(string propertyName, string message, object? attemptedValue = null)
    {
        Errors.Add(new ValidationError
        {
            PropertyName = propertyName,
            Message = message,
            AttemptedValue = attemptedValue,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Agrega un error de validación con código
    /// </summary>
    public void AddError(string propertyName, string message, string errorCode, object? attemptedValue = null)
    {
        Errors.Add(new ValidationError
        {
            PropertyName = propertyName,
            Message = message,
            ErrorCode = errorCode,
            AttemptedValue = attemptedValue,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Agrega una advertencia de validación
    /// </summary>
    public void AddWarning(string propertyName, string message, object? attemptedValue = null)
    {
        Warnings.Add(new ValidationWarning
        {
            PropertyName = propertyName,
            Message = message,
            AttemptedValue = attemptedValue,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Combina este resultado con otro
    /// </summary>
    public void Merge(ValidationResult other)
    {
        if (other == null) return;
        
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
    }

    /// <summary>
    /// Obtiene todos los errores para una propiedad específica
    /// </summary>
    public IEnumerable<ValidationError> GetErrorsForProperty(string propertyName)
    {
        return Errors.Where(e => string.Equals(e.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Obtiene todas las advertencias para una propiedad específica
    /// </summary>
    public IEnumerable<ValidationWarning> GetWarningsForProperty(string propertyName)
    {
        return Warnings.Where(w => string.Equals(w.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Limpia todos los errores y advertencias
    /// </summary>
    public void Clear()
    {
        Errors.Clear();
        Warnings.Clear();
    }

    /// <summary>
    /// Convierte el resultado a string con detalles
    /// </summary>
    public override string ToString()
    {
        var result = Summary;
        
        if (Errors.Any())
        {
            result += "\n\nErrores:";
            foreach (var error in Errors)
            {
                result += $"\n  • {error.PropertyName}: {error.Message}";
            }
        }
        
        if (Warnings.Any())
        {
            result += "\n\nAdvertencias:";
            foreach (var warning in Warnings)
            {
                result += $"\n  • {warning.PropertyName}: {warning.Message}";
            }
        }
        
        return result;
    }
}

/// <summary>
/// Representa un error de validación
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Nombre de la propiedad que falló la validación
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje descriptivo del error
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Código de error para identificación programática
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Valor que se intentó asignar
    /// </summary>
    public object? AttemptedValue { get; set; }

    /// <summary>
    /// Timestamp del error
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Severidad del error
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

/// <summary>
/// Representa una advertencia de validación
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Nombre de la propiedad que generó la advertencia
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje descriptivo de la advertencia
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Valor que generó la advertencia
    /// </summary>
    public object? AttemptedValue { get; set; }

    /// <summary>
    /// Timestamp de la advertencia
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Niveles de severidad para validación
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Información general
    /// </summary>
    Info,
    
    /// <summary>
    /// Advertencia - no bloquea la operación
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error - bloquea la operación
    /// </summary>
    Error,
    
    /// <summary>
    /// Error crítico - requiere atención inmediata
    /// </summary>
    Critical
}
