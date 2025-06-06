using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Models.Validation;
using CustomValidationResult = GestLog.Models.Validation.ValidationResult;

namespace GestLog.Services.Validation;

/// <summary>
/// Interfaz principal para el servicio de validación de datos
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Valida un objeto usando sus atributos de validación
    /// </summary>
    CustomValidationResult ValidateObject(object obj);

    /// <summary>
    /// Valida un objeto de forma asíncrona
    /// </summary>
    Task<CustomValidationResult> ValidateObjectAsync(object obj);

    /// <summary>
    /// Valida una propiedad específica de un objeto
    /// </summary>
    CustomValidationResult ValidateProperty(object obj, string propertyName, object? value);

    /// <summary>
    /// Registra un validador personalizado
    /// </summary>
    void RegisterValidator<T>(IValidator<T> validator);

    /// <summary>
    /// Obtiene validadores registrados para un tipo
    /// </summary>
    IEnumerable<IValidator<T>> GetValidators<T>();

    /// <summary>
    /// Valida archivos Excel específicamente
    /// </summary>
    Task<CustomValidationResult> ValidateExcelFileAsync(string filePath);

    /// <summary>
    /// Valida datos importados desde Excel
    /// </summary>
    CustomValidationResult ValidateExcelData(IEnumerable<Dictionary<string, object?>> data);
}

/// <summary>
/// Interfaz para validadores específicos por tipo
/// </summary>
public interface IValidator<T>
{
    /// <summary>
    /// Valida un objeto del tipo especificado
    /// </summary>
    ValidationResult Validate(T obj);

    /// <summary>
    /// Valida un objeto de forma asíncrona
    /// </summary>
    Task<ValidationResult> ValidateAsync(T obj);

    /// <summary>
    /// Indica si este validador puede manejar el objeto
    /// </summary>
    bool CanValidate(Type type);
}
