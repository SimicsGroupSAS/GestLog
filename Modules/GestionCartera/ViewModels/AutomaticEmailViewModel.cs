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
            };            if (openFileDialog.ShowDialog() == true)
            {
                SelectedEmailExcelFilePath = openFileDialog.FileName;
                HasEmailExcel = !string.IsNullOrEmpty(SelectedEmailExcelFilePath);
                
                _logger.LogInformation($"📧 Archivo de correos seleccionado: {Path.GetFileName(SelectedEmailExcelFilePath)}");
                
                await ValidateEmailExcelFileAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar archivo Excel de correos");
            LogText += $"\n❌ Error: {ex.Message}";
        }
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
    }

    private async Task ConfigureSmtpFromConfigAsync(SmtpConfigurationViewModel config)
    {
        if (_emailService == null) return;

        var smtpConfig = new SmtpConfiguration
        {
            SmtpServer = config.SmtpServer,
            Port = config.SmtpPort,
            Username = config.SmtpUsername,
            Password = config.SmtpPassword,
            EnableSsl = config.EnableSsl
        };

        await _emailService.ConfigureSmtpAsync(smtpConfig);
    }

    private async Task<bool> ProcessAutomaticEmailSendingAsync(
        IReadOnlyList<GeneratedPdfInfo> documents, 
        CancellationToken cancellationToken)
    {
        if (_emailService == null || _excelEmailService == null) return false;

        var emailsSent = 0;
        var emailsFailed = 0;
        var totalEmails = documents.Count;

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
                    cancellationToken);

                if (!emails.Any())
                {
                    LogText += $" ⚠️ Sin email";
                    emailsFailed++;
                    continue;
                }

                var emailInfo = new EmailInfo
                {
                    Recipients = emails.ToList(),
                    Subject = $"Estado de Cartera - {document.NombreEmpresa}",
                    Body = _emailService.GetEmailHtmlTemplate($"Estimado cliente,<br/><br/>Adjuntamos el estado de cartera correspondiente a su empresa <strong>{document.NombreEmpresa}</strong>.<br/><br/>Para cualquier consulta, no dude en contactarnos.<br/><br/>Cordialmente,"),
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
        }

        LogText += $"\n📊 Resumen final: {emailsSent}/{totalEmails} emails enviados exitosamente";
        if (emailsFailed > 0)
        {
            LogText += $", {emailsFailed} fallos";
        }

        return emailsSent > 0;
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

    // Este comando será llamado desde el MainViewModel con el parámetro correcto
    public async Task SendDocumentsAutomaticallyWithConfig(SmtpConfigurationViewModel smtpConfig)
    {
        await SendDocumentsAutomaticallyAsync(GeneratedDocuments, smtpConfig);
    }
}
