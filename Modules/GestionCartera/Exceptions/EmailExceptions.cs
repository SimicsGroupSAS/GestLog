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
}
