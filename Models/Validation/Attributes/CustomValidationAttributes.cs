using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;

namespace GestLog.Models.Validation.Attributes;

/// <summary>
/// Valida que un archivo existe y es accesible
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class FileExistsAttribute : ValidationAttribute
{
    public bool AllowEmpty { get; set; } = false;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

    public FileExistsAttribute() : base("El archivo especificado no existe o no es accesible.")
    {
    }

    public FileExistsAttribute(string errorMessage) : base(errorMessage)
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return AllowEmpty;
        }

        var filePath = value.ToString()!;
        
        if (!File.Exists(filePath))
        {
            return false;
        }

        if (AllowedExtensions.Length > 0)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return Array.Exists(AllowedExtensions, ext => ext.ToLowerInvariant() == extension);
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        if (AllowedExtensions.Length > 0)
        {
            return $"El archivo '{name}' debe existir y tener una extensión válida: {string.Join(", ", AllowedExtensions)}";
        }
        
        return $"El archivo '{name}' debe existir y ser accesible.";
    }
}

/// <summary>
/// Valida que un directorio existe
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class DirectoryExistsAttribute : ValidationAttribute
{
    public bool AllowEmpty { get; set; } = false;
    public bool CreateIfNotExists { get; set; } = false;

    public DirectoryExistsAttribute() : base("El directorio especificado no existe.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return AllowEmpty;
        }

        var directoryPath = value.ToString()!;
        
        if (!Directory.Exists(directoryPath))
        {
            if (CreateIfNotExists)
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"El directorio '{name}' debe existir.";
    }
}

/// <summary>
/// Valida rangos numéricos con soporte para diferentes tipos
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NumericRangeAttribute : ValidationAttribute
{
    public double Minimum { get; set; } = double.MinValue;
    public double Maximum { get; set; } = double.MaxValue;
    public bool ExclusiveMinimum { get; set; } = false;
    public bool ExclusiveMaximum { get; set; } = false;

    public NumericRangeAttribute(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Usar [Required] para validar null

        if (!double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
        {
            return false;
        }

        if (ExclusiveMinimum && numericValue <= Minimum) return false;
        if (!ExclusiveMinimum && numericValue < Minimum) return false;
        
        if (ExclusiveMaximum && numericValue >= Maximum) return false;
        if (!ExclusiveMaximum && numericValue > Maximum) return false;

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        var minOperator = ExclusiveMinimum ? ">" : ">=";
        var maxOperator = ExclusiveMaximum ? "<" : "<=";
        
        return $"El campo '{name}' debe estar entre {Minimum} {minOperator} valor {maxOperator} {Maximum}.";
    }
}

/// <summary>
/// Valida formatos de fecha específicos
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class DateFormatAttribute : ValidationAttribute
{
    public string[] AcceptedFormats { get; set; }
    public bool AllowEmpty { get; set; } = false;

    public DateFormatAttribute(params string[] acceptedFormats)
    {
        AcceptedFormats = acceptedFormats;
    }

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return AllowEmpty;
        }

        var dateString = value.ToString()!;
        
        if (AcceptedFormats?.Length > 0)
        {
            return DateTime.TryParseExact(dateString, AcceptedFormats, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }
        
        return DateTime.TryParse(dateString, out _);
    }

    public override string FormatErrorMessage(string name)
    {
        if (AcceptedFormats?.Length > 0)
        {
            return $"El campo '{name}' debe tener un formato de fecha válido: {string.Join(", ", AcceptedFormats)}.";
        }
        
        return $"El campo '{name}' debe contener una fecha válida.";
    }
}

/// <summary>
/// Valida que el valor no esté en una lista de valores prohibidos
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NotInAttribute : ValidationAttribute
{
    public object[] ProhibitedValues { get; set; }
    public bool IgnoreCase { get; set; } = false;

    public NotInAttribute(params object[] prohibitedValues)
    {
        ProhibitedValues = prohibitedValues;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true;

        foreach (var prohibited in ProhibitedValues)
        {
            if (IgnoreCase && value is string stringValue && prohibited is string prohibitedString)
            {
                if (string.Equals(stringValue, prohibitedString, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else if (Equals(value, prohibited))
            {
                return false;
            }
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"El campo '{name}' no puede contener ninguno de los siguientes valores: {string.Join(", ", ProhibitedValues)}.";
    }
}

/// <summary>
/// Valida que el valor esté en una lista de valores permitidos
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class AllowedValuesAttribute : ValidationAttribute
{
    public object[] AllowedValues { get; set; }
    public bool IgnoreCase { get; set; } = false;

    public AllowedValuesAttribute(params object[] allowedValues)
    {
        AllowedValues = allowedValues;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Usar [Required] para validar null

        foreach (var allowed in AllowedValues)
        {
            if (IgnoreCase && value is string stringValue && allowed is string allowedString)
            {
                if (string.Equals(stringValue, allowedString, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (Equals(value, allowed))
            {
                return true;
            }
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"El campo '{name}' debe contener uno de los siguientes valores: {string.Join(", ", AllowedValues)}.";
    }
}
