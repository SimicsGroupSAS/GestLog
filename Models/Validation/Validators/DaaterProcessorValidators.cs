using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestLog.Models.Configuration.Modules;
using GestLog.Models.Validation;
using GestLog.Services.Validation;

namespace GestLog.Models.Validation.Validators
{
    /// <summary>
    /// Validador específico para configuraciones del DaaterProcessor
    /// </summary>
    public class DaaterProcessorSettingsValidator : IValidator<DaaterProcessorSettings>
    {
        public ValidationResult Validate(DaaterProcessorSettings settings)
        {
            var result = new ValidationResult();

            if (settings == null)
            {
                result.AddError("Settings", "La configuración del DaaterProcessor no puede ser nula");
                return result;
            }

            ValidatePaths(settings, result);
            ValidateFileSettings(settings, result);
            ValidateFormatSettings(settings, result);
            ValidateProcessingSettings(settings, result);

            return result;
        }

        /// <summary>
        /// Implementación de validación asíncrona
        /// </summary>
        public async Task<ValidationResult> ValidateAsync(DaaterProcessorSettings settings)
        {
            // Podemos usar Task.Run para ejecutar la validación síncrona en otro hilo
            // o implementar alguna validación específica que requiera asincronía
            return await Task.Run(() => Validate(settings));
        }

        /// <summary>
        /// Verifica si este validador puede validar el tipo especificado
        /// </summary>
        public bool CanValidate(Type type)
        {
            // Este validador solo puede validar objetos DaaterProcessorSettings
            // o tipos que hereden de DaaterProcessorSettings
            return type != null && typeof(DaaterProcessorSettings).IsAssignableFrom(type);
        }

        private void ValidatePaths(DaaterProcessorSettings settings, ValidationResult result)
        {
            // Validar ruta de entrada
            if (!string.IsNullOrWhiteSpace(settings.DefaultInputPath))
            {
                try
                {
                    if (!Directory.Exists(settings.DefaultInputPath))
                    {
                        result.AddWarning("DefaultInputPath", 
                            $"El directorio de entrada no existe: {settings.DefaultInputPath}");
                    }
                }
                catch (Exception ex)
                {
                    result.AddError("DefaultInputPath", 
                        $"Ruta de entrada inválida: {ex.Message}");
                }
            }

            // Validar ruta de salida
            if (!string.IsNullOrWhiteSpace(settings.DefaultOutputPath))
            {
                try
                {
                    // Verificar si es una ruta absoluta o relativa
                    var fullPath = Path.IsPathRooted(settings.DefaultOutputPath) 
                        ? settings.DefaultOutputPath 
                        : Path.Combine(Environment.CurrentDirectory, settings.DefaultOutputPath);

                    var directory = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        result.AddWarning("DefaultOutputPath", 
                            $"El directorio padre de salida no existe: {directory}");
                    }
                }
                catch (Exception ex)
                {
                    result.AddError("DefaultOutputPath", 
                        $"Ruta de salida inválida: {ex.Message}");
                }
            }

            // Validar directorio de backup
            if (!string.IsNullOrWhiteSpace(settings.BackupDirectory))
            {
                try
                {
                    var fullBackupPath = Path.IsPathRooted(settings.BackupDirectory) 
                        ? settings.BackupDirectory 
                        : Path.Combine(Environment.CurrentDirectory, settings.BackupDirectory);

                    var directory = Path.GetDirectoryName(fullBackupPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        result.AddWarning("BackupDirectory", 
                            $"El directorio padre de backup no existe: {directory}");
                    }
                }
                catch (Exception ex)
                {
                    result.AddError("BackupDirectory", 
                        $"Directorio de backup inválido: {ex.Message}");
                }
            }
        }

        private void ValidateFileSettings(DaaterProcessorSettings settings, ValidationResult result)
        {
            // Validar máximo de filas por archivo
            if (settings.MaxRowsPerFile <= 0)
            {
                result.AddError("MaxRowsPerFile", 
                    "El número máximo de filas por archivo debe ser mayor a 0");
            }
            else if (settings.MaxRowsPerFile < 1000)
            {
                result.AddWarning("MaxRowsPerFile", 
                    "Un número muy bajo de filas por archivo puede generar muchos archivos pequeños");
            }
            else if (settings.MaxRowsPerFile > 10_000_000)
            {
                result.AddWarning("MaxRowsPerFile", 
                    "Un número muy alto de filas por archivo puede causar problemas de memoria");
            }
        }

        private void ValidateFormatSettings(DaaterProcessorSettings settings, ValidationResult result)
        {
            // Validar formato de fecha
            if (!string.IsNullOrWhiteSpace(settings.DateFormat))
            {
                try
                {
                    var testDate = DateTime.Now;
                    var formatted = testDate.ToString(settings.DateFormat);
                    
                    // Verificar formatos comunes recomendados
                    var commonFormats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy" };
                    if (!commonFormats.Contains(settings.DateFormat))
                    {
                        result.AddWarning("DateFormat", 
                            $"Formato de fecha no estándar: {settings.DateFormat}. Se recomiendan: {string.Join(", ", commonFormats)}");
                    }
                }
                catch (FormatException)
                {
                    result.AddError("DateFormat", 
                        $"Formato de fecha inválido: {settings.DateFormat}");
                }
            }

            // Validar separadores
            if (string.IsNullOrEmpty(settings.DecimalSeparator))
            {
                result.AddError("DecimalSeparator", 
                    "El separador decimal no puede estar vacío");
            }
            else if (settings.DecimalSeparator.Length > 1)
            {
                result.AddWarning("DecimalSeparator", 
                    "El separador decimal debería ser un solo carácter");
            }

            if (settings.DecimalSeparator == settings.ThousandsSeparator)
            {
                result.AddError("Separators", 
                    "El separador decimal y el separador de miles no pueden ser iguales");
            }

            // Validar separadores comunes
            var validDecimalSeparators = new[] { ".", "," };
            var validThousandsSeparators = new[] { ",", ".", " ", "'" };

            if (!validDecimalSeparators.Contains(settings.DecimalSeparator))
            {
                result.AddWarning("DecimalSeparator", 
                    $"Separador decimal no estándar: '{settings.DecimalSeparator}'. Se recomiendan: {string.Join(", ", validDecimalSeparators.Select(s => $"'{s}'"))}");
            }

            if (!string.IsNullOrEmpty(settings.ThousandsSeparator) && 
                !validThousandsSeparators.Contains(settings.ThousandsSeparator))
            {
                result.AddWarning("ThousandsSeparator", 
                    $"Separador de miles no estándar: '{settings.ThousandsSeparator}'. Se recomiendan: {string.Join(", ", validThousandsSeparators.Select(s => $"'{s}'"))}");
            }
        }

        private void ValidateProcessingSettings(DaaterProcessorSettings settings, ValidationResult result)
        {
            // Validaciones de consistencia lógica
            if (!settings.EnableDataConsolidation && settings.EnableProviderNormalization)
            {
                result.AddWarning("ProcessingSettings", 
                    "La normalización de proveedores está habilitada pero la consolidación de datos está deshabilitada. " +
                    "Esto puede no tener el efecto esperado.");
            }

            if (!settings.EnableDataConsolidation && settings.EnableCountryMapping)
            {
                result.AddWarning("ProcessingSettings", 
                    "El mapeo de países está habilitado pero la consolidación de datos está deshabilitada. " +
                    "Esto puede no tener el efecto esperado.");
            }

            if (settings.CreateBackupBeforeProcessing && string.IsNullOrWhiteSpace(settings.BackupDirectory))
            {
                result.AddError("BackupSettings", 
                    "El backup está habilitado pero no se ha especificado un directorio de backup");
            }

            // Validar configuraciones de rendimiento
            if (!settings.EnableProgressReporting && !settings.EnableErrorRecovery)
            {
                result.AddWarning("PerformanceSettings", 
                    "Tanto el reporte de progreso como la recuperación de errores están deshabilitados. " +
                    "Esto puede dificultar el diagnóstico de problemas.");
            }
        }
    }

    /// <summary>
    /// Validador para datos de archivos Excel procesados por DaaterProcessor
    /// </summary>
    public class ExcelDataValidator : IValidator<Dictionary<string, object?>>
    {
        private readonly string[] _requiredFields = { "Codigo", "Descripcion", "Cantidad", "Precio" };
        private readonly string[] _numericFields = { "Cantidad", "Precio", "Importe", "PrecioUnitario" };
        private readonly string[] _dateFields = { "Fecha", "FechaEntrega", "FechaVencimiento" };

        public ValidationResult Validate(Dictionary<string, object?> record)
        {
            var result = new ValidationResult();

            if (record == null || !record.Any())
            {
                result.AddError("Record", "El registro no puede estar vacío");
                return result;
            }

            ValidateRequiredFields(record, result);
            ValidateNumericFields(record, result);
            ValidateDateFields(record, result);
            ValidateBusinessRules(record, result);

            return result;
        }
        
        /// <summary>
        /// Implementación de validación asíncrona
        /// </summary>
        public async Task<ValidationResult> ValidateAsync(Dictionary<string, object?> record)
        {
            // Implementación simple que delega a la versión síncrona
            return await Task.Run(() => Validate(record));
        }
        
        /// <summary>
        /// Verifica si este validador puede validar el tipo especificado
        /// </summary>
        public bool CanValidate(Type type)
        {
            return type != null && 
                   typeof(Dictionary<string, object?>).IsAssignableFrom(type);
        }

        private void ValidateRequiredFields(Dictionary<string, object?> record, ValidationResult result)
        {
            foreach (var requiredField in _requiredFields)
            {
                var fieldKey = record.Keys.FirstOrDefault(k => 
                    k.Equals(requiredField, StringComparison.OrdinalIgnoreCase));

                if (fieldKey == null)
                {
                    result.AddError(requiredField, $"Campo requerido '{requiredField}' no encontrado");
                    continue;
                }

                var value = record[fieldKey];
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    result.AddError(fieldKey, $"El campo '{fieldKey}' es requerido y no puede estar vacío");
                }
            }
        }

        private void ValidateNumericFields(Dictionary<string, object?> record, ValidationResult result)
        {
            foreach (var numericField in _numericFields)
            {
                var fieldKey = record.Keys.FirstOrDefault(k => 
                    k.Equals(numericField, StringComparison.OrdinalIgnoreCase));

                if (fieldKey == null) continue;

                var value = record[fieldKey];
                if (value == null) continue;

                if (!decimal.TryParse(value.ToString(), out var numericValue))
                {
                    result.AddError(fieldKey, $"El campo '{fieldKey}' debe contener un valor numérico válido: {value}");
                }
                else
                {
                    // Validaciones específicas por campo
                    if (fieldKey.ToLowerInvariant().Contains("cantidad"))
                    {
                        if (numericValue < 0)
                        {
                            result.AddError(fieldKey, $"La cantidad no puede ser negativa: {numericValue}");
                        }
                        else if (numericValue == 0)
                        {
                            result.AddWarning(fieldKey, "Cantidad en cero detectada");
                        }
                    }

                    if (fieldKey.ToLowerInvariant().Contains("precio"))
                    {
                        if (numericValue < 0)
                        {
                            result.AddError(fieldKey, $"El precio no puede ser negativo: {numericValue}");
                        }
                        else if (numericValue == 0)
                        {
                            result.AddWarning(fieldKey, "Precio en cero detectado");
                        }
                        else if (numericValue > 1_000_000)
                        {
                            result.AddWarning(fieldKey, $"Precio muy alto detectado: {numericValue:C}");
                        }
                    }
                }
            }
        }

        private void ValidateDateFields(Dictionary<string, object?> record, ValidationResult result)
        {
            foreach (var dateField in _dateFields)
            {
                var fieldKey = record.Keys.FirstOrDefault(k => 
                    k.Equals(dateField, StringComparison.OrdinalIgnoreCase));

                if (fieldKey == null) continue;

                var value = record[fieldKey];
                if (value == null) continue;

                if (!DateTime.TryParse(value.ToString(), out var dateValue))
                {
                    result.AddError(fieldKey, $"El campo '{fieldKey}' debe contener una fecha válida: {value}");
                }
                else
                {
                    // Validaciones de rango de fechas
                    var now = DateTime.Now;
                    var minDate = new DateTime(1900, 1, 1);
                    var maxDate = now.AddYears(10);

                    if (dateValue < minDate)
                    {
                        result.AddWarning(fieldKey, $"Fecha muy antigua detectada: {dateValue:yyyy-MM-dd}");
                    }
                    else if (dateValue > maxDate)
                    {
                        result.AddWarning(fieldKey, $"Fecha muy futura detectada: {dateValue:yyyy-MM-dd}");
                    }

                    // Validaciones específicas por campo
                    if (fieldKey.ToLowerInvariant().Contains("entrega") && dateValue < now.Date)
                    {
                        result.AddWarning(fieldKey, $"Fecha de entrega en el pasado: {dateValue:yyyy-MM-dd}");
                    }

                    if (fieldKey.ToLowerInvariant().Contains("vencimiento") && dateValue < now.Date)
                    {
                        result.AddWarning(fieldKey, $"Fecha de vencimiento expirada: {dateValue:yyyy-MM-dd}");
                    }
                }
            }
        }

        private void ValidateBusinessRules(Dictionary<string, object?> record, ValidationResult result)
        {
            // Validar consistencia entre campos relacionados
            var cantidadKey = record.Keys.FirstOrDefault(k => k.ToLowerInvariant().Contains("cantidad"));
            var precioKey = record.Keys.FirstOrDefault(k => k.ToLowerInvariant().Contains("precio"));
            var importeKey = record.Keys.FirstOrDefault(k => k.ToLowerInvariant().Contains("importe"));

            if (cantidadKey != null && precioKey != null && importeKey != null)
            {
                var cantidad = record[cantidadKey];
                var precio = record[precioKey];
                var importe = record[importeKey];

                if (decimal.TryParse(cantidad?.ToString(), out var cantidadValue) &&
                    decimal.TryParse(precio?.ToString(), out var precioValue) &&
                    decimal.TryParse(importe?.ToString(), out var importeValue))
                {
                    var importeCalculado = cantidadValue * precioValue;
                    var diferencia = Math.Abs(importeCalculado - importeValue);
                    var tolerancia = Math.Max(0.01m, importeCalculado * 0.001m); // 0.1% o 1 centavo

                    if (diferencia > tolerancia)
                    {
                        result.AddWarning("BusinessRule", 
                            $"Inconsistencia en cálculo: Cantidad({cantidadValue}) × Precio({precioValue}) = {importeCalculado}, pero Importe = {importeValue}");
                    }
                }
            }

            // Validar códigos duplicados dentro del mismo registro
            var codigoKey = record.Keys.FirstOrDefault(k => k.ToLowerInvariant().Contains("codigo"));
            if (codigoKey != null)
            {
                var codigo = record[codigoKey]?.ToString();
                if (!string.IsNullOrWhiteSpace(codigo))
                {
                    // Verificar formato de código (ejemplo: debe ser alfanumérico)
                    if (!System.Text.RegularExpressions.Regex.IsMatch(codigo, @"^[a-zA-Z0-9\-_]+$"))
                    {
                        result.AddWarning(codigoKey, 
                            $"El código contiene caracteres no estándar: {codigo}");
                    }

                    // Verificar longitud razonable
                    if (codigo.Length > 50)
                    {
                        result.AddWarning(codigoKey, 
                            $"Código muy largo ({codigo.Length} caracteres): {codigo}");
                    }
                    else if (codigo.Length < 3)
                    {
                        result.AddWarning(codigoKey, 
                            $"Código muy corto ({codigo.Length} caracteres): {codigo}");
                    }
                }
            }
        }
    }
}
