using System;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.EnvioCatalogo.Models
{
    /// <summary>
    /// Configuración específica para el módulo de Envío de Catálogo
    /// </summary>
    public class EnvioCatalogoSettings
    {
        /// <summary>
        /// Servidor SMTP específico para Envío de Catálogo
        /// </summary>
        [Required(ErrorMessage = "El servidor SMTP es requerido")]
        public string SmtpServer { get; set; } = string.Empty;

        /// <summary>
        /// Puerto SMTP
        /// </summary>
        [Range(1, 65535, ErrorMessage = "El puerto debe estar entre 1 y 65535")]
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Usuario/Email para autenticación SMTP
        /// </summary>
        [Required(ErrorMessage = "El usuario SMTP es requerido")]
        [EmailAddress(ErrorMessage = "Debe ser una dirección de email válida")]
        public string SmtpUsername { get; set; } = string.Empty;

        /// <summary>
        /// Indica si SSL está habilitado
        /// </summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// Indica si el módulo está configurado correctamente
        /// </summary>
        public bool IsConfigured { get; set; } = false;

        /// <summary>
        /// Ruta por defecto del archivo de catálogo
        /// </summary>
        public string DefaultCatalogPath { get; set; } = "Data\\Catalogo Productos y Servicios Simics Group SAS.pdf";

        /// <summary>
        /// Asunto por defecto para los emails
        /// </summary>
        public string DefaultEmailSubject { get; set; } = "Catálogo de Productos y Servicios - SIMICS GROUP SAS";

        /// <summary>
        /// Timeout para envío de emails (milisegundos)
        /// </summary>
        [Range(1000, 300000, ErrorMessage = "El timeout debe estar entre 1000 y 300000 ms")]
        public int EmailTimeout { get; set; } = 30000;

        /// <summary>
        /// Delay entre envíos de emails (milisegundos) para evitar spam
        /// </summary>
        [Range(100, 10000, ErrorMessage = "El delay debe estar entre 100 y 10000 ms")]
        public int DelayBetweenEmails { get; set; } = 500;

        /// <summary>
        /// Fecha de última configuración
        /// </summary>
        public DateTime? LastConfigured { get; set; }

        /// <summary>
        /// Valida la configuración
        /// </summary>
        public bool ValidateConfiguration()
        {
            return !string.IsNullOrWhiteSpace(SmtpServer) &&
                   SmtpPort > 0 && SmtpPort <= 65535 &&
                   !string.IsNullOrWhiteSpace(SmtpUsername) &&
                   EmailTimeout > 0 &&
                   DelayBetweenEmails >= 0;
        }
    }
}
