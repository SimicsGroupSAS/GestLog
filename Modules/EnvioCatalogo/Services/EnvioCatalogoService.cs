using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Modules.EnvioCatalogo.Models;
using GestLog.Modules.EnvioCatalogo.Exceptions;
using GestLog.Modules.GestionCartera.Models; // Para reutilizar SmtpConfiguration
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.EnvioCatalogo.Services
{
    /// <summary>
    /// Servicio para envío de catálogo por email - Configuración SMTP independiente
    /// </summary>
    public class EnvioCatalogoService : IEnvioCatalogoService
    {
        private readonly IGestLogLogger _logger;
        private SmtpConfiguration? _smtpConfiguration;
        private readonly object _configurationLock = new object();

        // Configuración específica para el módulo de envío de catálogo
        private const string CONFIG_KEY_PREFIX = "EnvioCatalogo_SMTP_";

        public EnvioCatalogoService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Configura el SMTP específico para este módulo (independiente de Gestión de Cartera)
        /// </summary>
        public async Task ConfigureSmtpAsync(SmtpConfiguration configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📧 Configurando SMTP para Envío de Catálogo...");

                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                await ValidateSmtpConfigurationAsync(configuration, cancellationToken);

                lock (_configurationLock)
                {
                    _smtpConfiguration = configuration;
                }

                _logger.LogInformation("✅ SMTP configurado para Envío de Catálogo - Servidor: {Server}, Puerto: {Port}", 
                    configuration.SmtpServer, configuration.Port);
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al configurar SMTP para Envío de Catálogo");
                throw new CatalogoSmtpConfigurationException("No se pudo configurar el servicio SMTP para Envío de Catálogo", ex);
            }
        }        public async Task<List<string>> ReadEmailsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("📊 Leyendo emails desde Excel: {FilePath}", excelFilePath);
                    
                    // Validar archivo
                    if (!File.Exists(excelFilePath))
                        throw new CatalogoFileException("El archivo Excel no existe", excelFilePath);

                    if (!Path.GetExtension(excelFilePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                        throw new CatalogoExcelException("El archivo debe ser un Excel (.xlsx)", excelFilePath);

                    var emails = new List<string>();

                    using var workbook = new XLWorkbook(excelFilePath);
                    var worksheet = workbook.Worksheets.First();

                    var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                    
                    // Leer desde fila 2 (fila 1 son encabezados)
                    // Columna A: NOMBRE, B: NIT, C: CORREO
                    for (int row = 2; row <= rowCount; row++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var emailValue = worksheet.Cell(row, 3).GetValue<string>(); // Columna C = Correo
                        
                        if (!string.IsNullOrWhiteSpace(emailValue) && IsValidEmail(emailValue.Trim()))
                        {
                            emails.Add(emailValue.Trim());
                        }
                    }

                    _logger.LogInformation("✅ Leídos {Count} emails válidos desde Excel", emails.Count);
                    return emails.Distinct().ToList(); // Eliminar duplicados
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error leyendo emails desde Excel");
                    throw;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Lee información completa del cliente desde Excel (Nombre, NIT, Email)
        /// </summary>
        public async Task<List<CatalogoClientInfo>> ReadClientInfoFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("📊 Leyendo información de clientes desde Excel: {FilePath}", excelFilePath);
                    
                    // Validar archivo
                    if (!File.Exists(excelFilePath))
                        throw new CatalogoFileException("El archivo Excel no existe", excelFilePath);

                    if (!Path.GetExtension(excelFilePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                        throw new CatalogoExcelException("El archivo debe ser un Excel (.xlsx)", excelFilePath);

                    var clients = new List<CatalogoClientInfo>();

                    using var workbook = new XLWorkbook(excelFilePath);
                    var worksheet = workbook.Worksheets.First();

                    var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                    
                    // Leer desde fila 2 (fila 1 son encabezados)
                    // Columna A: NOMBRE, B: NIT, C: CORREO
                    for (int row = 2; row <= rowCount; row++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var nombre = worksheet.Cell(row, 1).GetValue<string>()?.Trim() ?? string.Empty; // Columna A
                        var nit = worksheet.Cell(row, 2).GetValue<string>()?.Trim() ?? string.Empty;    // Columna B
                        var email = worksheet.Cell(row, 3).GetValue<string>()?.Trim() ?? string.Empty;  // Columna C
                        
                        // Solo agregar si tiene email válido
                        if (!string.IsNullOrWhiteSpace(email) && IsValidEmail(email))
                        {
                            clients.Add(new CatalogoClientInfo
                            {
                                Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : "Estimados Señores",
                                NIT = nit,
                                Email = email
                            });
                        }
                    }

                    _logger.LogInformation("✅ Leídos {Count} clientes válidos desde Excel", clients.Count);
                    return clients;
                }                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error leyendo información de clientes desde Excel");
                    throw;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Alias de compatibilidad para ReadClientInfoFromExcelAsync
        /// </summary>
        public async Task<IEnumerable<CatalogoClientInfo>> ReadClientsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            var clients = await ReadClientInfoFromExcelAsync(excelFilePath, cancellationToken);
            return clients;
        }

        /// <summary>
        /// Envía un catálogo por email a un destinatario específico
        /// </summary>
        public async Task SendCatalogoEmailAsync(CatalogoEmailInfo emailInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                EnsureConfigured();
                ValidateCatalogoEmailInfo(emailInfo);

                if (emailInfo.Recipients == null || !emailInfo.Recipients.Any())
                {
                    throw new ArgumentException("No se han especificado destinatarios para el envío", nameof(emailInfo));
                }

                _logger.LogInformation("📧 Enviando catálogo por email a {Count} destinatarios", emailInfo.Recipients.Count);

                // Si hay múltiples destinatarios, usar el método múltiple
                if (emailInfo.Recipients.Count > 1)
                {
                    await SendCatalogoToMultipleRecipientsAsync(emailInfo, null, cancellationToken);
                }
                else
                {
                    // Un solo destinatario
                    var recipient = emailInfo.Recipients.First();
                    await SendSingleCatalogoEmailAsync(emailInfo, recipient, cancellationToken);
                    _logger.LogInformation("✅ Catálogo enviado exitosamente a: {Recipient}", recipient);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error enviando catálogo por email");
                throw;
            }
        }

        public async Task<CatalogoSendResult> SendCatalogoToMultipleRecipientsAsync(
            CatalogoEmailInfo emailInfo,
            IProgress<CatalogoProgressInfo>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                EnsureConfigured();
                ValidateCatalogoEmailInfo(emailInfo);

                _logger.LogInformation("🚀 Iniciando envío de catálogo a {Count} destinatarios", emailInfo.Recipients.Count);

                var progressInfo = new CatalogoProgressInfo
                {
                    TotalEmails = emailInfo.Recipients.Count,
                    StatusMessage = "Iniciando envío de catálogo..."
                };

                progress?.Report(progressInfo);

                var successfulSends = 0;
                var failedRecipients = new List<string>();

                for (int i = 0; i < emailInfo.Recipients.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var recipient = emailInfo.Recipients[i];
                    
                    progressInfo.ProcessedEmails = i;
                    progressInfo.CurrentRecipient = recipient;
                    progressInfo.StatusMessage = $"Enviando a {recipient}...";
                    progress?.Report(progressInfo);

                    try
                    {
                        await SendSingleCatalogoEmailAsync(emailInfo, recipient, cancellationToken);
                        successfulSends++;
                        progressInfo.SuccessfulSends = successfulSends;

                        _logger.LogInformation("✅ Catálogo enviado exitosamente a: {Recipient}", recipient);

                        // Pequeña pausa entre envíos para evitar spam
                        await Task.Delay(500, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error enviando catálogo a: {Recipient}", recipient);
                        failedRecipients.Add(recipient);
                        progressInfo.FailedSends = failedRecipients.Count;
                    }

                    progressInfo.ProcessedEmails = i + 1;
                    progress?.Report(progressInfo);
                }

                stopwatch.Stop();

                progressInfo.StatusMessage = "Envío completado";
                progress?.Report(progressInfo);

                var result = CatalogoSendResult.Success(emailInfo.Recipients.Count, successfulSends, stopwatch.Elapsed);
                result.FailedRecipients = failedRecipients;

                _logger.LogInformation("🎯 Envío de catálogo completado: {Successful}/{Total} exitosos en {Duration}",
                    successfulSends, emailInfo.Recipients.Count, stopwatch.Elapsed);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "❌ Error durante envío masivo de catálogo");
                return CatalogoSendResult.Error($"Error durante envío: {ex.Message}", emailInfo.Recipients?.Count ?? 0);
            }
        }

        public async Task<bool> SendTestEmailAsync(string recipient, CancellationToken cancellationToken = default)
        {
            try
            {
                EnsureConfigured();
                  if (!IsValidEmail(recipient))
                    throw new CatalogoEmailSendException($"Email inválido: {recipient}", recipient);

                _logger.LogInformation("🧪 Enviando email de prueba a: {Recipient}", recipient);

                var config = GetCurrentConfiguration();
                using var client = CreateSmtpClient();

                var message = new MailMessage
                {
                    From = new MailAddress(config.Username),
                    Subject = "Prueba de Configuración SMTP - Envío de Catálogo",
                    Body = GetTestEmailHtmlTemplate(),
                    IsBodyHtml = true
                };

                message.To.Add(recipient);

                await client.SendMailAsync(message);

                _logger.LogInformation("✅ Email de prueba enviado exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error enviando email de prueba");
                return false;
            }
        }

        public bool ValidateCatalogFile(string catalogFilePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(catalogFilePath))
                    return false;

                if (!File.Exists(catalogFilePath))
                    return false;

                var extension = Path.GetExtension(catalogFilePath);
                return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public string GetDefaultCatalogPath()
        {
            var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            return Path.Combine(dataPath, "Catalogo Productos y Servicios Simics Group SAS.pdf");
        }

        #region Métodos Privados

        private void EnsureConfigured()
        {            lock (_configurationLock)
            {
                if (_smtpConfiguration == null)
                    throw new CatalogoSmtpConfigurationException("El servicio SMTP no está configurado para Envío de Catálogo. Configure primero el SMTP.");
            }
        }

        private SmtpConfiguration GetCurrentConfiguration()
        {            lock (_configurationLock)
            {
                return _smtpConfiguration ?? throw new CatalogoSmtpConfigurationException("SMTP no configurado");
            }
        }        private async Task ValidateSmtpConfigurationAsync(SmtpConfiguration configuration, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(configuration.SmtpServer))
                throw new CatalogoSmtpConfigurationException("El servidor SMTP es requerido");

            if (configuration.Port <= 0 || configuration.Port > 65535)
                throw new CatalogoSmtpConfigurationException("El puerto debe estar entre 1 y 65535");

            if (string.IsNullOrWhiteSpace(configuration.Username))
                throw new CatalogoSmtpConfigurationException("El nombre de usuario es requerido");

            if (string.IsNullOrWhiteSpace(configuration.Password))
                throw new CatalogoSmtpConfigurationException("La contraseña es requerida");

            await Task.CompletedTask;
        }

        private void ValidateCatalogoEmailInfo(CatalogoEmailInfo emailInfo)
        {
            if (emailInfo == null)
                throw new ArgumentNullException(nameof(emailInfo));            if (emailInfo.Recipients == null || !emailInfo.Recipients.Any())
                throw new CatalogoEmailSendException("Debe especificar al menos un destinatario", "");

            foreach (var recipient in emailInfo.Recipients)
            {
                if (!IsValidEmail(recipient))
                    throw new CatalogoEmailSendException($"Dirección de email inválida: {recipient}", recipient);
            }

            if (!ValidateCatalogFile(emailInfo.CatalogFilePath))
                throw new CatalogoFileException("El archivo del catálogo no es válido o no existe", emailInfo.CatalogFilePath);
        }        private async Task SendSingleCatalogoEmailAsync(CatalogoEmailInfo emailInfo, string recipient, CancellationToken cancellationToken)
        {
            var config = GetCurrentConfiguration();
            using var client = CreateSmtpClient();

            // Generar el cuerpo personalizado del email
            var personalizedBody = GeneratePersonalizedEmailBody(emailInfo.CompanyName ?? "Estimados Señores");

            var message = new MailMessage
            {
                From = new MailAddress(config.Username),
                Subject = emailInfo.Subject,
                Body = personalizedBody,
                IsBodyHtml = true
            };

            message.To.Add(recipient);

            // Agregar copia oculta si está configurada
            if (!string.IsNullOrWhiteSpace(emailInfo.BccRecipient))
                message.Bcc.Add(emailInfo.BccRecipient);

            if (!string.IsNullOrWhiteSpace(emailInfo.CcRecipient))
                message.CC.Add(emailInfo.CcRecipient);

            // Adjuntar catálogo
            var attachment = new Attachment(emailInfo.CatalogFilePath);
            attachment.Name = "Catalogo_Productos_Servicios_SIMICS_GROUP.pdf";
            message.Attachments.Add(attachment);

            await client.SendMailAsync(message);
        }

        private SmtpClient CreateSmtpClient()
        {
            var config = GetCurrentConfiguration();
            return new System.Net.Mail.SmtpClient(config.SmtpServer)
            {
                Port = config.Port,
                Credentials = new System.Net.NetworkCredential(config.Username, config.Password),
                EnableSsl = config.EnableSsl,
                Timeout = config.Timeout
            };
        }

        private int FindEmailColumn(IXLWorksheet worksheet)
        {
            // Buscar en la primera fila columnas que contengan "email", "correo", "mail"
            var firstRow = worksheet.Row(1);
            var lastColumn = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 1;

            for (int col = 1; col <= lastColumn; col++)
            {
                var cellValue = firstRow.Cell(col).GetValue<string>().ToUpperInvariant();
                
                if (cellValue.Contains("EMAIL") || cellValue.Contains("CORREO") || cellValue.Contains("MAIL"))
                {
                    return col;
                }
            }

            return -1; // No encontrado
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }        private string GetTestEmailHtmlTemplate()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Prueba SMTP - Envío de Catálogo</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5;'>
    <div style='background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
        <h2 style='color: #2c3e50; margin-bottom: 20px;'>🧪 Prueba de Configuración SMTP</h2>
        
        <p style='color: #34495e; line-height: 1.6;'>
            Este es un email de prueba del módulo <strong>Envío de Catálogo</strong> de GestLog.
        </p>
        
        <p style='color: #34495e; line-height: 1.6;'>
            Si recibe este mensaje, significa que la configuración SMTP está funcionando correctamente.
        </p>
        
        <div style='background-color: #27ae60; color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <strong>✅ Configuración SMTP validada exitosamente</strong>
        </div>
        
        <hr style='border: none; border-top: 1px solid #ecf0f1; margin: 20px 0;'>
        
        <p style='color: #7f8c8d; font-size: 12px; text-align: center;'>
            <strong>SIMICS GROUP SAS</strong><br>
            Sistema GestLog - Módulo de Envío de Catálogo
        </p>
    </div>
</body>
</html>";
        }        /// <summary>
        /// Genera el cuerpo personalizado del email con la plantilla comercial
        /// </summary>
        private string GeneratePersonalizedEmailBody(string clientName)
        {        var template = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Importadores y Comercializadores de Aceros y Servicios - Simics Group SAS</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f8f9fa;'>
    <div style='max-width: 800px; margin: 0 auto; background-color: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
        
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #0f8937, #12a043); color: white; padding: 30px; border-radius: 8px 8px 0 0; text-align: center;'>
            <h1 style='margin: 0; font-size: 24px; font-weight: bold;'>SIMICS GROUP SAS</h1>
            <p style='margin: 5px 0 0 0; font-size: 16px; opacity: 0.9;'>Importadores y Comercializadores de Aceros y Servicios</p>
        </div>
        
        <!-- Content -->
        <div style='padding: 40px;'>
            <p style='font-size: 16px; color: #2c3e50; margin-bottom: 20px; line-height: 1.6;'>
                <strong>Buenos días Señores {CLIENT_NAME}</strong>
            </p>
            
            <p style='font-size: 14px; color: #34495e; margin-bottom: 20px; line-height: 1.6;'>
                Mi nombre es <strong>XXXXXX</strong>, de la empresa Simics Group SAS. Estamos ubicados en Barranquilla desde donde atendemos a toda la costa atlántica y el interior del país.
            </p>
            
            <p style='font-size: 14px; color: #34495e; margin-bottom: 20px; line-height: 1.6;'>
                El presente correo es para presentar nuestra empresa y ponerla a disposición de ustedes.
            </p>
            
            <div style='background-color: #f8f9fa; padding: 25px; border-radius: 6px; margin: 25px 0;'>
                <p style='font-size: 14px; color: #0f8937; margin-bottom: 15px; line-height: 1.7;'>
                    🟢 <strong>Somos importadores y comercializadores de aceros de todo tipo</strong>, tenemos material en stock suficiente para cubrir sus necesidades, sin embargo, también participamos en proyectos representando a las siderúrgicas más importantes de China, Japón, Turquía entre otros países.
                </p>
                
                <p style='font-size: 14px; color: #0f8937; margin-bottom: 10px; line-height: 1.7;'>
                    🟢 <strong>Podemos comercializar los siguientes productos:</strong>
                </p>
                
                <ul style='font-size: 14px; color: #34495e; line-height: 1.7; margin-left: 20px;'>
                    <li><strong>Láminas:</strong> A-36, A-283, A-131, A-572, A-516 GR 70, LAMINA ANTIDESGASTE 400-450HB, inoxidable y demás calidades especiales</li>
                    <li><strong>Perfilería:</strong> Ángulos, canales UPN, Vigas H, I, HEA, HEB, IPE, W</li>
                    <li><strong>Duraluminios</strong> en barra o platina</li>
                    <li><strong>Redondos:</strong> SAE 4140, 4340, 1045, 1020</li>
                    <li><strong>Barras perforadas</strong></li>
                </ul>
                
                <p style='font-size: 14px; color: #0f8937; margin: 15px 0; line-height: 1.7;'>
                    🟢 <strong>Importamos calidades especiales</strong> que no se consiguen en el mercado Colombiano. Puede consultarnos si tiene algún requerimiento puntual para consultarlo con los diferentes molinos.
                </p>
                
                <p style='font-size: 14px; color: #0f8937; margin: 15px 0; line-height: 1.7;'>
                    🟢 <strong>Realizamos trabajos de mecanizado:</strong> oxicortes, corte por plasma, láser, torno, fresadora.
                </p>
                
                <p style='font-size: 14px; color: #0f8937; margin: 15px 0; line-height: 1.7;'>
                    🟢 <strong>Comercializamos materiales de ferretería,</strong> soldaduras y repuestos para mantenimientos de plantas industriales.
                </p>
            </div>
            
            <div style='background-color: #e8f5e8; padding: 20px; border-left: 4px solid #0f8937; margin: 25px 0;'>
                <p style='font-size: 14px; color: #2c3e50; margin: 0; line-height: 1.7; font-weight: 500;'>
                    <strong>Nuestro valor agregado</strong> es que podemos atenderlos de una manera rápida y oportuna no solo vendiendo materiales sino realizando un acompañamiento técnico para cada uno de sus proyectos. Somos una empresa con un personal técnico y profesional que lleva más de <strong>40 años de experiencia</strong> en el sector.
                </p>
            </div>
            
            <p style='font-size: 14px; color: #34495e; margin: 20px 0; line-height: 1.6;'>
                Nos gustaría que nos invitaran a participar en presupuestos o cotizaciones de materiales que requieran. Queremos hacernos visibles para ustedes y que encuentren en nosotros un apoyo para cada una de sus operaciones.
            </p>
            
            <p style='font-size: 13px; color: #7f8c8d; margin: 25px 0; line-height: 1.6; font-style: italic;'>
                Este correo es emitido para fines comerciales, en caso de no ser la persona encargada agradecemos enviar este mensaje al responsable de compras / abastecimiento o compartirnos su correo electrónico para enviarle este comunicado.
            </p>
            
            <!-- Contact Info -->
            <div style='background: linear-gradient(135deg, #9d9d9c, #8a8a89); color: white; padding: 25px; border-radius: 6px; margin: 30px 0;'>
                <h3 style='margin: 0 0 15px 0; font-size: 18px; color: #ecf0f1;'>📞 Contacto</h3>
                <p style='margin: 8px 0; font-size: 14px; line-height: 1.6;'>
                    📧 <strong>Email:</strong> contactenos@simicsgroup.com
                </p>
                <p style='margin: 8px 0; font-size: 14px; line-height: 1.6;'>
                    📱 <strong>Celular:</strong> 315 224 05 20
                </p>
                <p style='margin: 8px 0; font-size: 14px; line-height: 1.6;'>
                    ☎️ <strong>Teléfono fijo:</strong> 605 329 55 05
                </p>
            </div>
            
        </div>
        
        <!-- Footer -->
        <div style='background-color: #ecf0f1; padding: 20px; border-radius: 0 0 8px 8px; text-align: center;'>
            <p style='margin: 0; font-size: 12px; color: #7f8c8d;'>
                <strong>SIMICS GROUP SAS</strong> - Más de 40 años de experiencia en el sector<br>
                Este mensaje fue enviado desde nuestro sistema automatizado de comunicaciones comerciales.
            </p>
        </div>
        
    </div>
</body>
</html>";

            return template.Replace("{CLIENT_NAME}", clientName);
        }

        #endregion
    }
}
