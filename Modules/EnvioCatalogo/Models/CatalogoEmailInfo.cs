using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.EnvioCatalogo.Models
{
    /// <summary>
    /// Información de un cliente desde Excel
    /// </summary>
    public class CatalogoClientInfo
    {
        /// <summary>
        /// Nombre del cliente/empresa
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// NIT del cliente
        /// </summary>
        public string NIT { get; set; } = string.Empty;

        /// <summary>
        /// Email del cliente
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Información para el envío de catálogo por email
    /// </summary>
    public class CatalogoEmailInfo
    {
        /// <summary>
        /// Lista de destinatarios para el envío
        /// </summary>
        [Required(ErrorMessage = "Debe especificar al menos un destinatario")]
        public List<string> Recipients { get; set; } = new List<string>();        /// <summary>
        /// Asunto del correo electrónico
        /// </summary>
        [Required(ErrorMessage = "El asunto es requerido")]
        public string Subject { get; set; } = "Importadores y Comercializadores de Aceros y Servicios - Simics Group SAS";

        /// <summary>
        /// Cuerpo del mensaje
        /// </summary>
        [Required(ErrorMessage = "El mensaje es requerido")]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el cuerpo del mensaje está en formato HTML
        /// </summary>
        public bool IsBodyHtml { get; set; } = true;

        /// <summary>
        /// Ruta al archivo del catálogo PDF
        /// </summary>
        [Required(ErrorMessage = "La ruta del catálogo es requerida")]
        public string CatalogFilePath { get; set; } = string.Empty;        /// <summary>
        /// Nombre de la empresa/destinatario (opcional, para personalización)
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// NIT del cliente
        /// </summary>
        public string? ClientNIT { get; set; }

        /// <summary>
        /// Email de copia oculta (BCC) opcional
        /// </summary>
        [EmailAddress(ErrorMessage = "Dirección BCC inválida")]
        public string? BccRecipient { get; set; }

        /// <summary>
        /// Email de copia (CC) opcional
        /// </summary>
        [EmailAddress(ErrorMessage = "Dirección CC inválida")]
        public string? CcRecipient { get; set; }
    }
}
