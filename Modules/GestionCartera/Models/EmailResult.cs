namespace GestLog.Modules.GestionCartera.Models
{
    /// <summary>
    /// Resultado del envío de correo electrónico
    /// </summary>
    public class EmailResult
    {
        /// <summary>
        /// Indica si el envío fue exitoso
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Mensaje de resultado
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Detalles del error si ocurrió
        /// </summary>
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// Cantidad de destinatarios procesados
        /// </summary>
        public int ProcessedRecipients { get; set; }

        /// <summary>
        /// Tamaño total de archivos adjuntos en KB
        /// </summary>
        public long TotalAttachmentSizeKb { get; set; }

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static EmailResult Success(string message, int processedRecipients, long attachmentSizeKb = 0)
        {
            return new EmailResult
            {
                IsSuccess = true,
                Message = message,
                ProcessedRecipients = processedRecipients,
                TotalAttachmentSizeKb = attachmentSizeKb
            };
        }

        /// <summary>
        /// Crea un resultado de error
        /// </summary>
        public static EmailResult Error(string message, string? errorDetails = null)
        {
            return new EmailResult
            {
                IsSuccess = false,
                Message = message,
                ErrorDetails = errorDetails
            };
        }
    }
}
