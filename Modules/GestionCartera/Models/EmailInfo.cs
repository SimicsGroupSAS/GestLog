using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.GestionCartera.Models
{
    /// <summary>
    /// Información del correo electrónico a enviar
    /// </summary>
    public class EmailInfo
    {
        /// <summary>
        /// Lista de destinatarios principales
        /// </summary>
        [Required(ErrorMessage = "Al menos un destinatario es requerido")]
        [MinLength(1, ErrorMessage = "Debe especificar al menos un destinatario")]
        public List<string> Recipients { get; set; } = new List<string>();

        /// <summary>
        /// Asunto del correo
        /// </summary>
        [Required(ErrorMessage = "El asunto es requerido")]
        [StringLength(998, ErrorMessage = "El asunto no puede exceder 998 caracteres")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Cuerpo del mensaje
        /// </summary>
        [Required(ErrorMessage = "El cuerpo del mensaje es requerido")]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el cuerpo es HTML
        /// </summary>
        public bool IsBodyHtml { get; set; } = true;

        /// <summary>
        /// Destinatario de copia (CC)
        /// </summary>
        [EmailAddress(ErrorMessage = "La dirección CC debe ser válida")]
        public string? CcRecipient { get; set; }

        /// <summary>
        /// Destinatario de copia oculta (BCC)
        /// </summary>
        [EmailAddress(ErrorMessage = "La dirección BCC debe ser válida")]
        public string? BccRecipient { get; set; }

        /// <summary>
        /// Rutas de archivos adjuntos
        /// </summary>
        public List<string> AttachmentPaths { get; set; } = new List<string>();
    }
}
