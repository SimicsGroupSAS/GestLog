using System;
using System.IO;

namespace GestLog.Modules.GestionCartera.Exceptions
{
    /// <summary>
    /// Excepción base para todos los errores específicos de gestión de documentos en GestLog
    /// </summary>
    public class GestLogDocumentException : Exception
    {
        public string ErrorCode { get; }

        public GestLogDocumentException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public GestLogDocumentException(string message, string errorCode, Exception? innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Excepción para errores de formato en documentos
    /// </summary>
    public class DocumentFormatException : GestLogDocumentException
    {
        public string FilePath { get; }
        public string ExpectedFormat { get; }

        public DocumentFormatException(string message, string filePath, string expectedFormat) 
            : base(message, "DOCUMENT_FORMAT_ERROR")
        {
            FilePath = filePath;
            ExpectedFormat = expectedFormat;
        }
        
        public DocumentFormatException(string message, string filePath, string expectedFormat, Exception? innerException) 
            : base(message, "DOCUMENT_FORMAT_ERROR", innerException)
        {
            FilePath = filePath;
            ExpectedFormat = expectedFormat;
        }
    }

    /// <summary>
    /// Excepción para errores de validación de archivos de documentos
    /// </summary>
    public class DocumentValidationException : GestLogDocumentException
    {
        public string FilePath { get; }
        public string ValidationRule { get; }

        public DocumentValidationException(string message, string filePath, string validationRule) 
            : base(message, "DOCUMENT_VALIDATION_ERROR")
        {
            FilePath = filePath;
            ValidationRule = validationRule;
        }
        
        public DocumentValidationException(string message, string filePath, string validationRule, Exception? innerException) 
            : base(message, "DOCUMENT_VALIDATION_ERROR", innerException)
        {
            FilePath = filePath;
            ValidationRule = validationRule;
        }
    }

    /// <summary>
    /// Excepción para errores de generación de PDF
    /// </summary>
    public class PdfGenerationException : GestLogDocumentException
    {
        public string? OutputPath { get; }
        
        public PdfGenerationException(string message, string? outputPath = null) 
            : base(message, "PDF_GENERATION_ERROR")
        {
            OutputPath = outputPath;
        }
        
        public PdfGenerationException(string message, string? outputPath, Exception? innerException) 
            : base(message, "PDF_GENERATION_ERROR", innerException)
        {
            OutputPath = outputPath;
        }
    }

    /// <summary>
    /// Excepción para errores de plantilla de documentos
    /// </summary>
    public class TemplateException : GestLogDocumentException
    {
        public string? TemplatePath { get; }
        
        public TemplateException(string message, string? templatePath = null) 
            : base(message, "TEMPLATE_ERROR")
        {
            TemplatePath = templatePath;
        }
        
        public TemplateException(string message, string? templatePath, Exception? innerException) 
            : base(message, "TEMPLATE_ERROR", innerException)
        {
            TemplatePath = templatePath;
        }
    }

    /// <summary>
    /// Excepción para errores de datos en los documentos
    /// </summary>
    public class DocumentDataException : GestLogDocumentException
    {
        public string? DataSource { get; }
        
        public DocumentDataException(string message, string? dataSource = null) 
            : base(message, "DOCUMENT_DATA_ERROR")
        {
            DataSource = dataSource;
        }
        
        public DocumentDataException(string message, string? dataSource, Exception? innerException) 
            : base(message, "DOCUMENT_DATA_ERROR", innerException)
        {
            DataSource = dataSource;
        }
    }
}
