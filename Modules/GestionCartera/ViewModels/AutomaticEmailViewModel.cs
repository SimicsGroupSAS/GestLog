using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.Models;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel para funcionalidades de email automático
/// </summary>
public partial class AutomaticEmailViewModel : ObservableObject
{
    private readonly IEmailService? _emailService;
    private readonly IExcelEmailService? _excelEmailService;
    private readonly IGestLogLogger _logger;

    [ObservableProperty] private string _selectedEmailExcelFilePath = string.Empty;
    [ObservableProperty] private bool _hasEmailExcel = false;
    [ObservableProperty] private bool _isSendingEmail = false;
    [ObservableProperty] private int _companiesWithEmail = 0;
    [ObservableProperty] private int _companiesWithoutEmail = 0;
    [ObservableProperty] private string _logText = string.Empty;
    
    // Propiedades adicionales necesarias para el wrapper
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _emailSubject = "Estado de Cartera - Documentos";
    [ObservableProperty] private string _emailBody = "Estimado cliente,\n\nAdjunto encontrará los documentos de estado de cartera solicitados.\n\nSaludos cordiales,\nSIMICS GROUP S.A.S.";
    [ObservableProperty] private string _emailRecipients = string.Empty;
    [ObservableProperty] private string _emailCc = string.Empty;
    [ObservableProperty] private string _emailBcc = string.Empty;
    [ObservableProperty] private bool _useHtmlEmail = true;
    [ObservableProperty] private bool _isEmailConfigured = false;
    [ObservableProperty] private IReadOnlyList<GeneratedPdfInfo> _generatedDocuments = new List<GeneratedPdfInfo>();

    public bool CanSendAutomatically => CanSendDocumentsAutomatically();

    public AutomaticEmailViewModel(
        IEmailService? emailService,
        IExcelEmailService? excelEmailService,
        IGestLogLogger logger)
    {
        _emailService = emailService;
        _excelEmailService = excelEmailService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }    [RelayCommand]
    public async Task SelectEmailExcelFileAsync()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar Archivo Excel con Correos Electrónicos"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedEmailExcelFilePath = openFileDialog.FileName;
                HasEmailExcel = !string.IsNullOrEmpty(SelectedEmailExcelFilePath);
                
                _logger.LogInformation($"📧 Archivo de correos seleccionado: {Path.GetFileName(SelectedEmailExcelFilePath)}");
                
                await ValidateEmailExcelFileAsync();
                
                // Analizar matching con documentos generados
                await AnalyzeEmailMatchingAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar archivo Excel de correos");
            LogText += $"\n❌ Error: {ex.Message}";
        }
    }    /// <summary>
    /// Analiza el matching entre documentos generados y correos del Excel
    /// </summary>
    private async Task AnalyzeEmailMatchingAsync()
    {
        if (_excelEmailService == null || string.IsNullOrEmpty(SelectedEmailExcelFilePath))
        {
            _logger.LogWarning("No se puede analizar matching: servicios o datos no disponibles");
            return;
        }

        try
        {
            _logger.LogInformation("📋 Cargando documentos generados desde pdfs_generados.txt...");
            LogText += "\n📋 Cargando documentos generados...";
            
            // Primero cargar los documentos desde el archivo de texto
            var documentsLoaded = await LoadGeneratedDocuments();
            if (!documentsLoaded.Any())
            {
                _logger.LogWarning("No se encontraron documentos generados para analizar");
                LogText += "\n⚠️ No se encontraron documentos generados";
                return;
            }

            _logger.LogInformation("🔍 Analizando matching entre {Count} documentos y correos del Excel...", documentsLoaded.Count);
            LogText += $"\n🔍 Analizando matching entre {documentsLoaded.Count} documentos y correos...";

            // Actualizar la lista de documentos generados
            GeneratedDocuments = documentsLoaded;

            int companiesWithEmailCount = 0;
            int companiesWithoutEmailCount = 0;

            foreach (var document in GeneratedDocuments)
            {
                try
                {
                    var emails = await _excelEmailService.GetEmailsForCompanyAsync(
                        SelectedEmailExcelFilePath,
                        document.NombreEmpresa,
                        document.Nit,
                        CancellationToken.None);

                    if (emails.Any())
                    {
                        companiesWithEmailCount++;
                        _logger.LogDebug("✅ {Company} tiene {EmailCount} email(s)", document.NombreEmpresa, emails.Count);
                        LogText += $"\n  ✅ {document.NombreEmpresa}: {emails.Count} email(s)";
                    }
                    else
                    {
                        companiesWithoutEmailCount++;
                        _logger.LogDebug("❌ {Company} sin email", document.NombreEmpresa);
                        LogText += $"\n  ❌ {document.NombreEmpresa}: sin email";
                    }
                }
                catch (Exception ex)
                {
                    companiesWithoutEmailCount++;
                    _logger.LogWarning(ex, "Error analizando {Company}", document.NombreEmpresa);
                }
            }

            CompaniesWithEmail = companiesWithEmailCount;
            CompaniesWithoutEmail = companiesWithoutEmailCount;

            _logger.LogInformation("📊 Análisis completado: {WithEmail} con email, {WithoutEmail} sin email", 
                companiesWithEmailCount, companiesWithoutEmailCount);
            LogText += $"\n📊 Resultado: {companiesWithEmailCount} con email, {companiesWithoutEmailCount} sin email";

            // Actualizar el estado para habilitar/deshabilitar envío automático
            OnPropertyChanged(nameof(CanSendAutomatically));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante análisis de matching");
            LogText += $"\n❌ Error durante análisis: {ex.Message}";
        }
    }

    /// <summary>
    /// Carga los documentos generados desde el archivo pdfs_generados.txt
    /// </summary>
    private async Task<List<GeneratedPdfInfo>> LoadGeneratedDocuments()
    {
        var documents = new List<GeneratedPdfInfo>();
        
        try
        {
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archivos", "Clientes cartera pdf");
            var textFilePath = Path.Combine(outputPath, "pdfs_generados.txt");
            
            if (!File.Exists(textFilePath))
            {
                _logger.LogWarning("No se encontró archivo pdfs_generados.txt en: {Path}", textFilePath);
                return documents;
            }

            _logger.LogInformation("📖 Leyendo archivo de documentos generados: {FilePath}", textFilePath);
            
            var lines = await File.ReadAllLinesAsync(textFilePath) ?? Array.Empty<string>();
            
            string? empresa = null;
            string? nit = null;
            string? archivo = null;
            string? tipo = null;
            string? ruta = null;
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // Líneas de separación o información general
                if (line.StartsWith("=") || line.StartsWith("PDF") || line.StartsWith("Total") || 
                    line.StartsWith("Fecha de generación") || line.StartsWith("--------") || 
                    line.Trim() == "-------------------------------------------------------------")
                    continue;
                
                if (line.StartsWith("Empresa: "))
                {
                    empresa = line.Replace("Empresa: ", "").Trim();
                }
                else if (line.StartsWith("NIT: "))
                {
                    nit = line.Replace("NIT: ", "").Trim();
                }
                else if (line.StartsWith("Archivo: "))
                {
                    archivo = line.Replace("Archivo: ", "").Trim();
                }
                else if (line.StartsWith("Tipo: "))
                {
                    tipo = line.Replace("Tipo: ", "").Trim();
                }
                else if (line.StartsWith("Ruta: "))
                {
                    ruta = line.Replace("Ruta: ", "").Trim();
                    
                    // Si tenemos ruta, es el final de un bloque de documento
                    if (!string.IsNullOrEmpty(empresa) && !string.IsNullOrEmpty(nit) && 
                        !string.IsNullOrEmpty(archivo) && !string.IsNullOrEmpty(ruta))
                    {
                        // Verificar que el archivo existe físicamente
                        if (File.Exists(ruta))
                        {
                            var document = new GeneratedPdfInfo
                            {
                                NombreArchivo = archivo,
                                NombreEmpresa = empresa,
                                Nit = nit,
                                RutaArchivo = ruta
                            };
                            
                            documents.Add(document);
                            _logger.LogDebug("📄 Documento cargado: {Archivo} - {Empresa} (NIT: {Nit})", 
                                archivo, empresa, nit);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Archivo no encontrado: {FilePath}", ruta);
                        }
                    }
                    
                    // Resetear variables para el siguiente documento
                    empresa = null;
                    nit = null;
                    archivo = null;
                    tipo = null;
                    ruta = null;
                }
            }
            
            _logger.LogInformation("✅ Documentos cargados desde archivo: {Count} documentos", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error leyendo archivo de documentos generados");
        }
        
        return documents;
    }

    public async Task<bool> SendDocumentsAutomaticallyAsync(
        IReadOnlyList<GeneratedPdfInfo> documents, 
        SmtpConfigurationViewModel smtpConfig,
        CancellationToken cancellationToken = default)
    {        if (_emailService == null || _excelEmailService == null)
        {
            _logger.LogWarning("Servicios de email no disponibles para envío automático");
            return false;
        }

        if (!documents.Any())
        {
            _logger.LogWarning("No hay documentos para enviar");
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedEmailExcelFilePath))
        {
            _logger.LogWarning("No hay archivo Excel seleccionado para mapear emails");
            return false;
        }

        try
        {
            IsSendingEmail = true;
            LogText += "\n🚀 Iniciando envío automático de documentos...\n";

            // Configurar SMTP
            await ConfigureSmtpFromConfigAsync(smtpConfig);

            // Procesar envíos
            var result = await ProcessAutomaticEmailSendingAsync(documents, cancellationToken);

            LogText += result ? "\n✅ Envío automático completado" : "\n❌ Envío automático falló";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el envío automático");
            LogText += $"\n❌ Error durante envío automático: {ex.Message}";
            return false;
        }
        finally
        {
            IsSendingEmail = false;
        }
    }

    private async Task ValidateEmailExcelFileAsync()
    {
        try
        {
            if (_excelEmailService == null)
            {
                _logger.LogWarning("⚠️ Servicio ExcelEmailService no disponible");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedEmailExcelFilePath) || !File.Exists(SelectedEmailExcelFilePath))
            {
                _logger.LogWarning("❌ Archivo Excel de correos no existe o no está seleccionado");
                LogText += $"\n❌ Error: Archivo de correos no válido";
                HasEmailExcel = false;
                SelectedEmailExcelFilePath = string.Empty;
                return;
            }

            // Validar contenido
            var testCompanies = await _excelEmailService.GetEmailsForCompanyAsync(
                SelectedEmailExcelFilePath, 
                "TEST_COMPANY", 
                "TEST_NIT", 
                CancellationToken.None);

            _logger.LogInformation("✅ Archivo Excel de correos válido y accesible");
            LogText += $"\n✅ Archivo de correos válido: {Path.GetFileName(SelectedEmailExcelFilePath)}";
            HasEmailExcel = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar archivo Excel de correos");
            LogText += $"\n❌ Error validando archivo de correos: {ex.Message}";
            HasEmailExcel = false;
        }
    }    private async Task ConfigureSmtpFromConfigAsync(SmtpConfigurationViewModel config)
    {
        if (_emailService == null) return;

        var smtpConfig = new SmtpConfiguration
        {
            SmtpServer = config.SmtpServer,
            Port = config.SmtpPort,
            Username = config.SmtpUsername,
            Password = config.SmtpPassword,
            EnableSsl = config.EnableSsl,
            BccEmail = config.BccEmail,
            CcEmail = config.CcEmail
        };

        await _emailService.ConfigureSmtpAsync(smtpConfig);
    }

    private async Task<bool> ProcessAutomaticEmailSendingAsync(
        IReadOnlyList<GeneratedPdfInfo> documents, 
        CancellationToken cancellationToken)
    {
        if (_emailService == null || _excelEmailService == null) return false;        var emailsSent = 0;
        var emailsFailed = 0;
        var orphansSent = 0;
        var totalEmails = documents.Count;

        // Obtener la configuración BCC para documentos huérfanos
        var smtpConfig = _emailService.CurrentConfiguration;
        var bccEmail = smtpConfig?.BccEmail;

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                LogText += $"\n  📄 Procesando: {document.NombreArchivo} ({document.NombreEmpresa})";

                var emails = await _excelEmailService.GetEmailsForCompanyAsync(
                    SelectedEmailExcelFilePath, 
                    document.NombreEmpresa, 
                    document.Nit, 
                    cancellationToken);                if (!emails.Any())
                {
                    // Documento huérfano - enviar al BCC configurado si existe
                    if (!string.IsNullOrWhiteSpace(bccEmail))
                    {                        LogText += $" 📧 Sin Correo → BCC";
                          var orphanEmailInfo = new EmailInfo
                        {
                            Recipients = new List<string> { bccEmail }, // Enviar al BCC como destinatario principal
                            Subject = $"Estado de Cartera - Sin Correo Destinatario - {document.NombreEmpresa}",
                            Body = GetOrphanEmailBodyWithSignature(document.NombreEmpresa, document.Nit),
                            IsBodyHtml = true
                        };

                        var orphanResult = await _emailService.SendEmailWithAttachmentAsync(orphanEmailInfo, document.RutaArchivo, cancellationToken);
                        
                        if (orphanResult.IsSuccess)
                        {
                            orphansSent++;
                            LogText += $" ✅ Enviado al BCC";
                        }
                        else
                        {
                            emailsFailed++;
                            LogText += $" ❌ Error BCC: {orphanResult.Message}";
                        }
                    }
                    else
                    {
                        LogText += $" ⚠️ Sin email y sin BCC configurado";
                        emailsFailed++;
                    }
                    continue;
                }                var emailInfo = new EmailInfo
                {
                    Recipients = emails.ToList(),
                    Subject = "Estado Cartera - SIMICS GROUP S.A.S",
                    Body = GetCompleteEmailBodyWithSignature(),
                    IsBodyHtml = true
                };

                var result = await _emailService.SendEmailWithAttachmentAsync(emailInfo, document.RutaArchivo, cancellationToken);
                
                if (result.IsSuccess)
                {
                    emailsSent++;
                    LogText += $" ✅ Enviado a {emails.Count} destinatario(s)";
                }
                else
                {
                    emailsFailed++;
                    LogText += $" ❌ Error: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                emailsFailed++;
                LogText += $" ❌ Error: {ex.Message}";
            }
        }        LogText += $"\n📊 Resumen final: {emailsSent}/{totalEmails} emails enviados exitosamente";
        if (orphansSent > 0)
        {
            LogText += $", {orphansSent} sin correo al BCC";
        }
        if (emailsFailed > 0)
        {
            LogText += $", {emailsFailed} fallos";
        }

        return emailsSent > 0 || orphansSent > 0;
    }

    /// <summary>
    /// Determina si se puede enviar automáticamente
    /// </summary>
    private bool CanSendDocumentsAutomatically()
    {
        return !IsSendingEmail && 
               IsEmailConfigured && 
               HasEmailExcel && 
               GeneratedDocuments.Count > 0;
    }

    /// <summary>
    /// Actualiza la configuración de email
    /// </summary>
    public void UpdateEmailConfiguration(bool isConfigured)
    {
        IsEmailConfigured = isConfigured;
        OnPropertyChanged(nameof(CanSendAutomatically));
    }

    /// <summary>
    /// Actualiza la lista de documentos generados
    /// </summary>
    public void UpdateGeneratedDocuments(IReadOnlyList<GeneratedPdfInfo> documents)
    {
        GeneratedDocuments = documents;
        OnPropertyChanged(nameof(CanSendAutomatically));
    }    /// <summary>
    /// Limpia recursos
    /// </summary>
    public void Cleanup()
    {
        // Limpiar recursos si es necesario
    }

    /// <summary>
    /// Genera el cuerpo completo del email con toda la firma HTML (idéntico al proyecto de implementación)
    /// </summary>
    private string GetCompleteEmailBodyWithSignature()
    {
        return @"<div style='font-family: Arial, sans-serif; line-height: 1.6; text-align: justify;'>
<p>Para SIMICS GROUP S.A.S. es muy importante contar con clientes como usted e informar constantemente la situación de cartera que tenemos a la fecha.</p>

<p>Adjuntamos estado de cuenta, en caso de tener alguna factura vencida agradecemos su colaboración con la programación de pagos.</p>

<p>Si tiene alguna observación agradecemos informarla por este medio para su revisión.</p>

<p>En caso de no ser la persona encargada agradecemos enviar este mensaje al responsable o compartirnos su correo electrónico para enviarle este comunicado.</p>

<p>Muchas gracias por su ayuda.</p>

<p>Cordialmente,</p>

<table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
  <tbody>
    <tr>
      <td>
        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;width:385px'>
          <tbody>
            <tr>
              <td width='80' style='vertical-align:middle'>
                <span style='margin-right:20px;display:block'>
                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_Icono2021Firma.png' role='presentation' width='80' style='max-width:80px'>
                </span>
              </td>
              <td style='vertical-align:middle'>
                <h3 style='margin:0;font-size:14px;color:#000'>
                  <span>JUAN MANUEL</span> <span>CUERVO PINILLA</span>
                </h3>
                <p style='margin:0;font-weight:500;color:#000;font-size:12px;line-height:15px'>
                  <span>Gerente Financiero</span>
                </p>
                <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                  <tbody>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image002.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <a href='tel:+34654623277' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>+34-654623277</span>
                        </a> |
                        <a href='tel:+573163114545' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>+57-3163114545</span>
                        </a>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image003.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <a href='mailto:juan.cuervo@simicsgroup.com' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>juan.cuervo@simicsgroup.com</span>
                        </a>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image004.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <span style='font-size:11px;color:#000'>
                          <span>CR 53 No. 96-24 Oficina 3D</span>
                        </span>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'></td>
                      <td style='padding:0;color:#000'>
                        <span style='font-size:11px;color:#000'>
                          <span>Barranquilla, Colombia</span>
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
    <tr>
      <td>
        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;width:385px'>
          <tbody>
            <tr height='60' style='vertical-align:middle'>
              <th style='width:100%'>
                <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_2021-1Firma.png' width='200' style='max-width:200px;display:inline-block'>
              </th>
            </tr>
            <tr height='25' style='text-align:center'>
              <td style='width:100%'>
                <a href='https://www.simicsgroup.com/' style='text-decoration:none;color:#000;font-size:11px;text-align:center'>
                  <span>www.simicsgroup.com</span>
                </a>
              </td>
            </tr>
            <tr height='25' style='text-align:center'>
              <td style='text-align:center;vertical-align:top'>
                <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;display:inline-block'>
                  <tbody>
                    <tr style='text-align:right'>
                      <td>
                        <a href='https://www.linkedin.com/company/simicsgroupsas' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image006.png' alt='linkedin' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                      <td>
                        <a href='https://www.instagram.com/simicsgroupsas/' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image007.png' alt='instagram' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                      <td>
                        <a href='https://www.facebook.com/SIMICSGroupSAS/' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image008.png' alt='facebook' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
  </tbody>
</table>
</div>";    }    /// <summary>
    /// Genera el cuerpo de email para documentos sin correo destinatario con firma completa
    /// </summary>
    private string GetOrphanEmailBodyWithSignature(string empresaName, string nit)
    {
        return @"<div style='font-family: Arial, sans-serif; line-height: 1.6; text-align: justify;'>
<p><strong>DOCUMENTOS SIN CORREO ELECTRÓNICO DESTINATARIO</strong></p>

<p>Se adjuntan los documentos de estado de cartera para los cuales no se encontró correo electrónico registrado:</p>

<p>Empresa: <strong>" + empresaName + @"</strong><br/>
NIT: " + nit + @"</p>

<p>Cordialmente,</p>

<table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
  <tbody>
    <tr>
      <td>
        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;width:385px'>
          <tbody>
            <tr>
              <td width='80' style='vertical-align:middle'>
                <span style='margin-right:20px;display:block'>
                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_Icono2021Firma.png' role='presentation' width='80' style='max-width:80px'>
                </span>
              </td>
              <td style='vertical-align:middle'>
                <h3 style='margin:0;font-size:14px;color:#000'>
                  <span>JUAN MANUEL</span> <span>CUERVO PINILLA</span>
                </h3>
                <p style='margin:0;font-weight:500;color:#000;font-size:12px;line-height:15px'>
                  <span>Gerente Financiero</span>
                </p>
                <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                  <tbody>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image002.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <a href='tel:+34654623277' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>+34-654623277</span>
                        </a> |
                        <a href='tel:+573163114545' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>+57-3163114545</span>
                        </a>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image003.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <a href='mailto:juan.cuervo@simicsgroup.com' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>juan.cuervo@simicsgroup.com</span>
                        </a>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image004.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <span style='font-size:11px;color:#000'>
                          <span>CR 53 No. 96-24 Oficina 3D</span>
                        </span>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'></td>
                      <td style='padding:0;color:#000'>
                        <span style='font-size:11px;color:#000'>
                          <span>Barranquilla, Colombia</span>
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
    <tr>
      <td>
        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;width:385px'>
          <tbody>
            <tr height='60' style='vertical-align:middle'>
              <th style='width:100%'>
                <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_2021-1Firma.png' width='200' style='max-width:200px;display:inline-block'>
              </th>
            </tr>
            <tr height='25' style='text-align:center'>
              <td style='width:100%'>
                <a href='https://www.simicsgroup.com/' style='text-decoration:none;color:#000;font-size:11px;text-align:center'>
                  <span>www.simicsgroup.com</span>
                </a>
              </td>
            </tr>
            <tr height='25' style='text-align:center'>
              <td style='text-align:center;vertical-align:top'>
                <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;display:inline-block'>
                  <tbody>
                    <tr style='text-align:right'>
                      <td>
                        <a href='https://www.linkedin.com/company/simicsgroupsas' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image006.png' alt='linkedin' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                      <td>
                        <a href='https://www.instagram.com/simicsgroupsas/' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image007.png' alt='instagram' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                      <td>
                        <a href='https://www.facebook.com/SIMICSGroupSAS/' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image008.png' alt='facebook' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
  </tbody>
</table>
</div>";
    }

    // Este comando será llamado desde el MainViewModel con el parámetro correcto
    public async Task<bool> SendDocumentsAutomaticallyWithConfig(SmtpConfigurationViewModel smtpConfig)
    {
        return await SendDocumentsAutomaticallyAsync(GeneratedDocuments, smtpConfig);
    }
}
