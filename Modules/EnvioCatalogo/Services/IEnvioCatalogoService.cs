using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.EnvioCatalogo.Models;
using GestLog.Modules.GestionCartera.Models;

namespace GestLog.Modules.EnvioCatalogo.Services
{    /// <summary>
    /// Interfaz para el servicio de envío de catálogo por email
    /// </summary>
    public interface IEnvioCatalogoService
    {
        /// <summary>
        /// Configura el SMTP específico para este módulo (independiente de Gestión de Cartera)
        /// </summary>
        /// <param name="configuration">Configuración SMTP</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task de configuración</returns>
        Task ConfigureSmtpAsync(SmtpConfiguration configuration, CancellationToken cancellationToken = default);        /// <summary>
        /// Lee las direcciones de email desde un archivo Excel
        /// </summary>
        /// <param name="excelFilePath">Ruta al archivo Excel</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de direcciones de email válidas</returns>
        Task<List<string>> ReadEmailsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default);        /// <summary>
        /// Lee información completa de clientes desde Excel (Nombre, NIT, Email)
        /// </summary>
        /// <param name="excelFilePath">Ruta al archivo Excel</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de información de clientes</returns>
        Task<List<CatalogoClientInfo>> ReadClientInfoFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lee información completa de clientes desde Excel (alias para compatibilidad)
        /// </summary>
        /// <param name="excelFilePath">Ruta al archivo Excel</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de información de clientes</returns>
        Task<IEnumerable<CatalogoClientInfo>> ReadClientsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía el catálogo por email a múltiples destinatarios
        /// </summary>
        /// <param name="emailInfo">Información del email y destinatarios</param>
        /// <param name="progress">Reporte de progreso opcional</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado del envío con estadísticas</returns>
        Task<CatalogoSendResult> SendCatalogoToMultipleRecipientsAsync(
            CatalogoEmailInfo emailInfo,
            IProgress<CatalogoProgressInfo>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un catálogo por email a un destinatario específico
        /// </summary>
        /// <param name="emailInfo">Información del email</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task de envío</returns>
        Task SendCatalogoEmailAsync(CatalogoEmailInfo emailInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un email de prueba
        /// </summary>
        /// <param name="recipient">Destinatario del email de prueba</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si el envío fue exitoso</returns>
        Task<bool> SendTestEmailAsync(string recipient, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida que el archivo del catálogo existe y es accesible
        /// </summary>
        /// <param name="catalogFilePath">Ruta al archivo del catálogo</param>
        /// <returns>True si el archivo es válido</returns>
        bool ValidateCatalogFile(string catalogFilePath);

        /// <summary>
        /// Obtiene la ruta por defecto del catálogo
        /// </summary>
        /// <returns>Ruta completa al archivo del catálogo</returns>
        string GetDefaultCatalogPath();
    }

    /// <summary>
    /// Resultado del envío del catálogo
    /// </summary>
    public class CatalogoSendResult
    {
        public bool IsSuccess { get; set; }
        public int TotalEmails { get; set; }
        public int SuccessfulSends { get; set; }
        public int FailedSends { get; set; }
        public List<string> FailedRecipients { get; set; } = new List<string>();
        public string Message { get; set; } = string.Empty;
        public System.TimeSpan Duration { get; set; }

        public static CatalogoSendResult Success(int totalEmails, int successful, System.TimeSpan duration)
        {
            return new CatalogoSendResult
            {
                IsSuccess = true,
                TotalEmails = totalEmails,
                SuccessfulSends = successful,
                FailedSends = totalEmails - successful,
                Duration = duration,
                Message = $"Catálogo enviado exitosamente a {successful} de {totalEmails} destinatarios"
            };
        }

        public static CatalogoSendResult Error(string message, int totalEmails = 0)
        {
            return new CatalogoSendResult
            {
                IsSuccess = false,
                TotalEmails = totalEmails,
                Message = message
            };
        }
    }

    /// <summary>
    /// Información de progreso del envío
    /// </summary>
    public class CatalogoProgressInfo
    {
        public int TotalEmails { get; set; }
        public int ProcessedEmails { get; set; }
        public int SuccessfulSends { get; set; }
        public int FailedSends { get; set; }
        public string CurrentRecipient { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public double PercentageComplete => TotalEmails > 0 ? (double)ProcessedEmails / TotalEmails * 100 : 0;
    }
}
