using System;

namespace GestLog.Modules.DaaterProccesor.Exceptions
{
    /// <summary>
    /// Excepción base para todos los errores específicos de GestLog
    /// </summary>
    public class GestLogException : Exception
    {
        public string ErrorCode { get; }

        public GestLogException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public GestLogException(string message, string errorCode, Exception? innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Excepción para errores de formato en archivos Excel
    /// </summary>
    public class ExcelFormatException : GestLogException
    {
        public string FilePath { get; }
        public string ExpectedFormat { get; }

        public ExcelFormatException(string message, string filePath, string expectedFormat) 
            : base(message, "EXCEL_FORMAT_ERROR")
        {
            FilePath = filePath;
            ExpectedFormat = expectedFormat;
        }
        
        public ExcelFormatException(string message, string filePath, string expectedFormat, Exception? innerException) 
            : base(message, "EXCEL_FORMAT_ERROR", innerException)
        {
            FilePath = filePath;
            ExpectedFormat = expectedFormat;
        }
    }

    /// <summary>
    /// Excepción para errores de validación de archivos
    /// </summary>
    public class FileValidationException : GestLogException
    {
        public string FilePath { get; }
        public string ValidationRule { get; }

        public FileValidationException(string message, string filePath, string validationRule) 
            : base(message, "FILE_VALIDATION_ERROR")
        {
            FilePath = filePath;
            ValidationRule = validationRule;
        }
        
        public FileValidationException(string message, string filePath, string validationRule, Exception? innerException) 
            : base(message, "FILE_VALIDATION_ERROR", innerException)
        {
            FilePath = filePath;
            ValidationRule = validationRule;
        }
    }

    /// <summary>
    /// Excepción para errores relacionados con recursos faltantes o inválidos
    /// </summary>
    public class ResourceException : GestLogException
    {
        public string ResourceName { get; }

        public ResourceException(string message, string resourceName) 
            : base(message, "RESOURCE_ERROR")
        {
            ResourceName = resourceName;
        }
        
        public ResourceException(string message, string resourceName, Exception? innerException) 
            : base(message, "RESOURCE_ERROR", innerException)
        {
            ResourceName = resourceName;
        }
    }

    /// <summary>
    /// Excepción para errores en la estructura de datos de Excel
    /// </summary>
    public class ExcelDataException : GestLogException
    {
        public string FilePath { get; }
        
        public ExcelDataException(string message, string filePath) 
            : base(message, "EXCEL_DATA_ERROR")
        {
            FilePath = filePath;
        }
        
        public ExcelDataException(string message, string filePath, Exception? innerException) 
            : base(message, "EXCEL_DATA_ERROR", innerException)
        {
            FilePath = filePath;
        }
    }
}
