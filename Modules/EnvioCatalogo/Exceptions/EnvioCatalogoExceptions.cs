using System;

namespace GestLog.Modules.EnvioCatalogo.Exceptions
{
    /// <summary>
    /// Excepción base para el módulo de Envío de Catálogo
    /// </summary>
    public abstract class EnvioCatalogoException : Exception
    {
        public string ErrorCode { get; }

        protected EnvioCatalogoException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected EnvioCatalogoException(string message, string errorCode, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Excepción para errores de configuración SMTP del módulo
    /// </summary>
    public class CatalogoSmtpConfigurationException : EnvioCatalogoException
    {
        public CatalogoSmtpConfigurationException(string message) 
            : base(message, "CATALOGO_SMTP_CONFIG_ERROR")
        {
        }

        public CatalogoSmtpConfigurationException(string message, Exception innerException) 
            : base(message, "CATALOGO_SMTP_CONFIG_ERROR", innerException)
        {
        }
    }

    /// <summary>
    /// Excepción para errores relacionados con el archivo del catálogo
    /// </summary>
    public class CatalogoFileException : EnvioCatalogoException
    {
        public string FilePath { get; }

        public CatalogoFileException(string message, string filePath) 
            : base(message, "CATALOGO_FILE_ERROR")
        {
            FilePath = filePath;
        }

        public CatalogoFileException(string message, string filePath, Exception innerException) 
            : base(message, "CATALOGO_FILE_ERROR", innerException)
        {
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Excepción para errores de envío de email
    /// </summary>
    public class CatalogoEmailSendException : EnvioCatalogoException
    {
        public string RecipientEmail { get; }

        public CatalogoEmailSendException(string message, string recipientEmail) 
            : base(message, "CATALOGO_EMAIL_SEND_ERROR")
        {
            RecipientEmail = recipientEmail;
        }

        public CatalogoEmailSendException(string message, string recipientEmail, Exception innerException) 
            : base(message, "CATALOGO_EMAIL_SEND_ERROR", innerException)
        {
            RecipientEmail = recipientEmail;
        }
    }

    /// <summary>
    /// Excepción para errores de lectura de Excel
    /// </summary>
    public class CatalogoExcelException : EnvioCatalogoException
    {
        public string ExcelFilePath { get; }

        public CatalogoExcelException(string message, string excelFilePath) 
            : base(message, "CATALOGO_EXCEL_ERROR")
        {
            ExcelFilePath = excelFilePath;
        }

        public CatalogoExcelException(string message, string excelFilePath, Exception innerException) 
            : base(message, "CATALOGO_EXCEL_ERROR", innerException)
        {
            ExcelFilePath = excelFilePath;
        }
    }
}
