using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Logging;
using GestLog.Models.Validation;
using CustomValidationResult = GestLog.Models.Validation.ValidationResult;

namespace GestLog.Services.Validation;

/// <summary>
/// Implementación del servicio de validación de datos
/// </summary>
public class ValidationService : IValidationService
{
    private readonly IGestLogLogger _logger;
    private readonly ConcurrentDictionary<Type, List<object>> _validators = new();

    public ValidationService(IGestLogLogger logger)
    {
        _logger = logger ?? LoggingService.GetLogger();
    }    /// <summary>
    /// Valida un objeto usando sus atributos de validación
    /// </summary>
    public CustomValidationResult ValidateObject(object obj)
    {
        var result = new CustomValidationResult();
        
        if (obj == null)
        {
            result.AddError("Object", "El objeto a validar no puede ser nulo");
            return result;
        }

        try
        {
            var validationContext = new ValidationContext(obj);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Validar usando DataAnnotations
            var isValid = Validator.TryValidateObject(obj, validationContext, validationResults, true);

            foreach (var validationResult in validationResults)
            {
                var propertyName = validationResult.MemberNames.FirstOrDefault() ?? "Unknown";
                result.AddError(propertyName, validationResult.ErrorMessage ?? "Error de validación");
            }

            // Ejecutar validadores personalizados
            ExecuteCustomValidators(obj, result);

            _logger.LogDebug("✅ Validación de objeto completada: {Type}, Errores: {ErrorCount}", 
                obj.GetType().Name, result.ErrorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la validación del objeto {Type}", obj.GetType().Name);
            result.AddError("ValidationService", $"Error interno de validación: {ex.Message}");
        }

        return result;
    }    /// <summary>
    /// Valida un objeto de forma asíncrona
    /// </summary>
    public async Task<CustomValidationResult> ValidateObjectAsync(object obj)
    {
        return await Task.Run(() => ValidateObject(obj));
    }

    /// <summary>
    /// Valida una propiedad específica de un objeto
    /// </summary>
    public CustomValidationResult ValidateProperty(object obj, string propertyName, object? value)
    {
        var result = new CustomValidationResult();
        
        if (obj == null)
        {
            result.AddError(propertyName, "El objeto no puede ser nulo");
            return result;
        }

        try
        {
            var validationContext = new ValidationContext(obj) { MemberName = propertyName };
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            var isValid = Validator.TryValidateProperty(value, validationContext, validationResults);

            foreach (var validationResult in validationResults)
            {
                result.AddError(propertyName, validationResult.ErrorMessage ?? "Error de validación de propiedad");
            }

            _logger.LogDebug("✅ Validación de propiedad completada: {PropertyName}, Errores: {ErrorCount}", 
                propertyName, result.ErrorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la validación de la propiedad {PropertyName}", propertyName);
            result.AddError(propertyName, $"Error interno de validación: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Registra un validador personalizado
    /// </summary>
    public void RegisterValidator<T>(IValidator<T> validator)
    {
        if (validator == null) throw new ArgumentNullException(nameof(validator));

        var type = typeof(T);
        _validators.AddOrUpdate(type, 
            new List<object> { validator },
            (key, existing) => 
            {
                existing.Add(validator);
                return existing;
            });

        _logger.LogDebug("📝 Validador registrado para tipo: {Type}", type.Name);
    }

    /// <summary>
    /// Obtiene validadores registrados para un tipo
    /// </summary>
    public IEnumerable<IValidator<T>> GetValidators<T>()
    {
        var type = typeof(T);
        if (_validators.TryGetValue(type, out var validators))
        {
            return validators.OfType<IValidator<T>>();
        }
        return Enumerable.Empty<IValidator<T>>();
    }    /// <summary>
    /// Valida archivos Excel específicamente
    /// </summary>
    public async Task<CustomValidationResult> ValidateExcelFileAsync(string filePath)
    {
        var result = new CustomValidationResult();

        try
        {
            _logger.LogDebug("📊 Iniciando validación de archivo Excel: {FilePath}", filePath);

            // Validaciones básicas del archivo
            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.AddError("FilePath", "La ruta del archivo no puede estar vacía");
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.AddError("FilePath", $"El archivo no existe: {filePath}");
                return result;
            }

            var fileInfo = new FileInfo(filePath);
            
            // Validar extensión
            var allowedExtensions = new[] { ".xlsx", ".xls", ".xlsm" };
            if (!allowedExtensions.Contains(fileInfo.Extension.ToLowerInvariant()))
            {
                result.AddError("FileExtension", $"Extensión de archivo no soportada: {fileInfo.Extension}");
                return result;
            }

            // Validar tamaño del archivo (máximo 100MB)
            if (fileInfo.Length > 100 * 1024 * 1024)
            {
                result.AddWarning("FileSize", $"El archivo es muy grande: {fileInfo.Length / (1024 * 1024)} MB");
            }

            // Validar contenido del Excel
            await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook(filePath);
                    
                    if (workbook.Worksheets.Count == 0)
                    {
                        result.AddError("WorksheetCount", "El archivo Excel no contiene hojas de trabajo");
                        return;
                    }

                    foreach (var worksheet in workbook.Worksheets)
                    {
                        // Validar que la hoja no esté vacía
                        var usedRange = worksheet.RangeUsed();
                        if (usedRange == null)
                        {
                            result.AddWarning("EmptyWorksheet", $"La hoja '{worksheet.Name}' está vacía");
                            continue;
                        }

                        // Validar estructura básica
                        var rowCount = usedRange.RowCount();
                        var columnCount = usedRange.ColumnCount();

                        if (rowCount < 2)
                        {
                            result.AddWarning("InsufficientRows", 
                                $"La hoja '{worksheet.Name}' tiene muy pocas filas: {rowCount}");
                        }

                        if (columnCount < 1)
                        {
                            result.AddError("InsufficientColumns", 
                                $"La hoja '{worksheet.Name}' no tiene columnas válidas");
                        }

                        _logger.LogDebug("📋 Hoja '{WorksheetName}': {RowCount} filas, {ColumnCount} columnas", 
                            worksheet.Name, rowCount, columnCount);
                    }
                }
                catch (Exception ex)
                {
                    result.AddError("FileContent", $"Error al leer el contenido del archivo: {ex.Message}");
                }
            });

            _logger.LogDebug("✅ Validación de Excel completada: {FilePath}, Errores: {ErrorCount}, Advertencias: {WarningCount}", 
                filePath, result.ErrorCount, result.WarningCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la validación del archivo Excel: {FilePath}", filePath);
            result.AddError("ValidationService", $"Error interno durante la validación: {ex.Message}");
        }

        return result;
    }    /// <summary>
    /// Valida datos importados desde Excel
    /// </summary>
    public CustomValidationResult ValidateExcelData(IEnumerable<Dictionary<string, object?>> data)
    {
        var result = new CustomValidationResult();

        try
        {
            var dataList = data.ToList();
            _logger.LogDebug("📊 Iniciando validación de datos Excel: {RecordCount} registros", dataList.Count);

            if (!dataList.Any())
            {
                result.AddWarning("EmptyData", "No hay datos para validar");
                return result;
            }

            var recordIndex = 0;
            foreach (var record in dataList)
            {
                recordIndex++;
                ValidateExcelRecord(record, recordIndex, result);
            }

            _logger.LogDebug("✅ Validación de datos Excel completada: {RecordCount} registros, {ErrorCount} errores", 
                dataList.Count, result.ErrorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la validación de datos Excel");
            result.AddError("ValidationService", $"Error interno durante la validación: {ex.Message}");
        }

        return result;
    }    /// <summary>
    /// Ejecuta validadores personalizados para un objeto
    /// </summary>
    private void ExecuteCustomValidators(object obj, CustomValidationResult result)
    {
        var objectType = obj.GetType();
        
        // Buscar validadores para el tipo exacto
        if (_validators.TryGetValue(objectType, out var validators))        {
            foreach (var validator in validators)
            {
                try
                {
                    var method = validator.GetType().GetMethod("Validate");
                    if (method != null)
                    {
                        var validationResult = method.Invoke(validator, new[] { obj }) as CustomValidationResult;
                        if (validationResult != null)
                        {
                            result.Merge(validationResult);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error en validador personalizado para {Type}", objectType.Name);
                    result.AddError("CustomValidator", $"Error en validador personalizado: {ex.Message}");
                }
            }
        }

        // Buscar validadores para tipos base
        var baseType = objectType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (_validators.TryGetValue(baseType, out var baseValidators))
            {
                foreach (var validator in baseValidators)
                {
                    try                    {
                        if (validator.GetType().GetMethod("CanValidate")?.Invoke(validator, new object[] { objectType }) is true)
                        {
                            var method = validator.GetType().GetMethod("Validate");
                            if (method != null)
                            {
                                var validationResult = method.Invoke(validator, new[] { obj }) as CustomValidationResult;
                                if (validationResult != null)
                                {
                                    result.Merge(validationResult);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error en validador base para {Type}", baseType.Name);
                    }
                }
            }
            baseType = baseType.BaseType;
        }
    }    /// <summary>
    /// Valida un registro individual de Excel
    /// </summary>
    private void ValidateExcelRecord(Dictionary<string, object?> record, int recordIndex, CustomValidationResult result)
    {
        var recordPrefix = $"Registro[{recordIndex}]";

        // Validar que el registro no esté completamente vacío
        if (!record.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString())))
        {
            result.AddWarning(recordPrefix, "El registro está completamente vacío");
            return;
        }

        // Validaciones específicas para campos comunes en GestLog
        foreach (var kvp in record)
        {
            var fieldName = kvp.Key;
            var value = kvp.Value;
            var propertyName = $"{recordPrefix}.{fieldName}";

            // Validar campos de fecha
            if (fieldName.ToLowerInvariant().Contains("fecha") && value != null)
            {
                if (!DateTime.TryParse(value.ToString(), out _))
                {
                    result.AddError(propertyName, $"Formato de fecha inválido: {value}");
                }
            }

            // Validar campos numéricos
            if ((fieldName.ToLowerInvariant().Contains("cantidad") || 
                 fieldName.ToLowerInvariant().Contains("precio") ||
                 fieldName.ToLowerInvariant().Contains("importe")) && value != null)
            {
                if (!decimal.TryParse(value.ToString(), out var numValue))
                {
                    result.AddError(propertyName, $"Valor numérico inválido: {value}");
                }
                else if (numValue < 0)
                {
                    result.AddWarning(propertyName, $"Valor negativo detectado: {numValue}");
                }
            }

            // Validar campos de código
            if (fieldName.ToLowerInvariant().Contains("codigo") && value != null)
            {
                var codeValue = value.ToString();
                if (string.IsNullOrWhiteSpace(codeValue))
                {
                    result.AddError(propertyName, "El código no puede estar vacío");
                }
                else if (codeValue.Length > 50)
                {
                    result.AddWarning(propertyName, $"Código muy largo: {codeValue.Length} caracteres");
                }
            }
        }
    }
}
