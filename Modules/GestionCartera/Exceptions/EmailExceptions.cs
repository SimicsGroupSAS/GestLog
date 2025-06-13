using System;

namespace GestLog.Modules.GestionCartera.Exceptions
{
    /// <summary>
    /// Excepción base para errores relacionados con el envío de correos electrónicos
    /// </summary>
    public class EmailException : GestLogDocumentException
    {
        public EmailException(string message) 
            : base(message, "EMAIL_ERROR") { }
        
        public EmailException(string message, Exception innerException) 
            : base(message, "EMAIL_ERROR", innerException) { }
    }

    /// <summary>
    /// Excepción para errores de configuración SMTP
    /// </summary>
    public class SmtpConfigurationException : EmailException
    {
        public SmtpConfigurationException(string message) : base($"Error de configuración SMTP: {message}") { }
        
        public SmtpConfigurationException(string message, Exception innerException) : base($"Error de configuración SMTP: {message}", innerException) { }
    }

    /// <summary>
    /// Excepción para errores de autenticación SMTP
    /// </summary>
    public class SmtpAuthenticationException : EmailException
    {
        public SmtpAuthenticationException(string message) : base($"Error de autenticación SMTP: {message}") { }
        
        public SmtpAuthenticationException(string message, Exception innerException) : base($"Error de autenticación SMTP: {message}", innerException) { }
    }

    /// <summary>
    /// Excepción para errores de archivos adjuntos
    /// </summary>
    public class AttachmentException : EmailException
    {
        public string? FilePath { get; }

        public AttachmentException(string message, string? filePath = null) : base(message)
        {
            FilePath = filePath;
        }
        
        public AttachmentException(string message, string? filePath, Exception innerException) : base(message, innerException)
        {
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Excepción para errores de destinatarios de correo
    /// </summary>
    public class RecipientException : EmailException
    {
        public string? RecipientEmail { get; }

        public RecipientException(string message, string? recipientEmail = null) : base(message)
        {
            RecipientEmail = recipientEmail;
        }
        
        public RecipientException(string message, string? recipientEmail, Exception innerException) : base(message, innerException)
        {
            RecipientEmail = recipientEmail;
        }
    }

    /// <summary>
    /// Excepción para errores de validación de archivos Excel de correos
    /// </summary>
    public class EmailExcelValidationException : EmailException
    {
        public string? FilePath { get; }
        public string? ExpectedFormat { get; }

        public EmailExcelValidationException(string message, string? filePath = null, string? expectedFormat = null) 
            : base($"Error de validación de archivo Excel de correos: {message}")
        {
            FilePath = filePath;
            ExpectedFormat = expectedFormat;
        }
          public EmailExcelValidationException(string message, string? filePath, string? expectedFormat, Exception innerException) 
            : base($"Error de validación de archivo Excel de correos: {message}", innerException)
        {
            FilePath = filePath;
            ExpectedFormat = expectedFormat;
        }
    }

    /// <summary>
    /// Excepción para archivos Excel de correos con estructura incorrecta
    /// </summary>
    public class EmailExcelStructureException : EmailException
    {
        public string? FilePath { get; }
        public string? ExpectedFormat { get; }
        public string[] MissingColumns { get; }
        public string[] FoundColumns { get; }

        public EmailExcelStructureException(string message, string? filePath, string[] missingColumns, string[] foundColumns) 
            : base($"Estructura de archivo Excel incorrecta: {message}")
        {
            FilePath = filePath;
            ExpectedFormat = "Columnas requeridas: TIPO_DOC, NUM_ID, DIGITO_VER, EMPRESA, EMAIL";
            MissingColumns = missingColumns ?? Array.Empty<string>();
            FoundColumns = foundColumns ?? Array.Empty<string>();
        }
        
        public EmailExcelStructureException(string message, string? filePath, string[] missingColumns, string[] foundColumns, Exception innerException) 
            : base($"Estructura de archivo Excel incorrecta: {message}", innerException)
        {
            FilePath = filePath;
            ExpectedFormat = "Columnas requeridas: TIPO_DOC, NUM_ID, DIGITO_VER, EMPRESA, EMAIL";
            MissingColumns = missingColumns ?? Array.Empty<string>();
            FoundColumns = foundColumns ?? Array.Empty<string>();
        }
    }
}
