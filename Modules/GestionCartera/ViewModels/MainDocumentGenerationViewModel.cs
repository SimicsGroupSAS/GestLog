using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;
using GestLog.Services.Configuration;
using GestLog.Modules.GestionCartera.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel principal que orquesta todos los componentes de generación de documentos
/// </summary>
public partial class MainDocumentGenerationViewModel : ObservableObject
{
    private readonly IGestLogLogger _logger;
    private readonly IConfigurationService _configurationService;

    // ViewModels especializados
    public PdfGenerationViewModel PdfGeneration { get; }
    public DocumentManagementViewModel DocumentManagement { get; }
    public SmtpConfigurationViewModel SmtpConfiguration { get; }
    public AutomaticEmailViewModel AutomaticEmail { get; }

    // Propiedades de estado general
    [ObservableProperty] private string _logText = string.Empty;
    [ObservableProperty] private string _globalStatusMessage = "Listo para generar documentos";

    public MainDocumentGenerationViewModel(
        IPdfGeneratorService pdfGenerator,
        IEmailService emailService,
        IGestLogLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Obtener servicios del contenedor DI
        var serviceProvider = LoggingService.GetServiceProvider();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        var credentialService = serviceProvider.GetRequiredService<ICredentialService>();
        var excelEmailService = serviceProvider.GetService<IExcelEmailService>();        // Inicializar ViewModels especializados
        PdfGeneration = new PdfGenerationViewModel(pdfGenerator, logger);
        DocumentManagement = new DocumentManagementViewModel(logger);
        SmtpConfiguration = new SmtpConfigurationViewModel(emailService, _configurationService, credentialService, logger);
        AutomaticEmail = new AutomaticEmailViewModel(emailService, excelEmailService, logger);

        // Suscribirse a eventos de los ViewModels
        SubscribeToViewModelEvents();
        
        // Suscribirse a cambios de configuración
        _configurationService.ConfigurationChanged += OnConfigurationChanged;

        _logger.LogInformation("🚀 MainDocumentGenerationViewModel inicializado correctamente");
    }

    /// <summary>
    /// Suscribirse a eventos de los ViewModels especializados
    /// </summary>
    private void SubscribeToViewModelEvents()
    {
        // Eventos de generación de PDF
        PdfGeneration.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PdfGeneration.StatusMessage))
            {
                GlobalStatusMessage = PdfGeneration.StatusMessage;
            }
            else if (e.PropertyName == nameof(PdfGeneration.LogText))
            {
                LogText += PdfGeneration.LogText;
            }
            else if (e.PropertyName == nameof(PdfGeneration.GeneratedDocuments))
            {
                // Sincronizar documentos generados con el gestor de documentos
                DocumentManagement.UpdateGeneratedDocuments(PdfGeneration.GeneratedDocuments);
                
                // Actualizar el ViewModel de email automático
                AutomaticEmail.UpdateGeneratedDocuments(PdfGeneration.GeneratedDocuments);
            }
        };

        // Eventos de gestión de documentos
        DocumentManagement.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DocumentManagement.StatusMessage))
            {
                GlobalStatusMessage = DocumentManagement.StatusMessage;
            }
            else if (e.PropertyName == nameof(DocumentManagement.GeneratedDocuments))
            {
                // Sincronizar con el ViewModel de email automático
                AutomaticEmail.UpdateGeneratedDocuments(DocumentManagement.GeneratedDocuments);
            }
        };

        // Eventos de configuración SMTP
        SmtpConfiguration.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SmtpConfiguration.StatusMessage))
            {
                GlobalStatusMessage = SmtpConfiguration.StatusMessage;
            }
            else if (e.PropertyName == nameof(SmtpConfiguration.IsEmailConfigured))
            {
                // Sincronizar configuración con el ViewModel de email automático
                AutomaticEmail.UpdateEmailConfiguration(SmtpConfiguration.IsEmailConfigured);
            }
        };        // Eventos de email automático
        AutomaticEmail.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AutomaticEmail.StatusMessage))
            {
                GlobalStatusMessage = AutomaticEmail.StatusMessage;
            }
            else if (e.PropertyName == nameof(AutomaticEmail.LogText))
            {
                LogText += AutomaticEmail.LogText;
            }
        };
    }    /// <summary>
    /// Maneja cambios en la configuración
    /// </summary>
    private async void OnConfigurationChanged(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("🔄 Configuración cambiada, sincronizando ViewModels...");
            
            // Recargar configuración en los ViewModels correspondientes
            await SmtpConfiguration.LoadSmtpConfigurationAsync();
            
            _logger.LogInformation("✅ ViewModels sincronizados con nueva configuración");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sincronizando configuración");
        }
    }

    /// <summary>
    /// Limpia el log de texto
    /// </summary>
    [RelayCommand]
    private void ClearLog()
    {
        LogText = string.Empty;
        GlobalStatusMessage = "Log limpiado";
        _logger.LogInformation("🧹 Log de texto limpiado");
    }

    /// <summary>
    /// Inicializa todos los componentes después de la construcción
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Inicializando componentes del MainDocumentGenerationViewModel...");
            
            // Cargar configuración SMTP
            await SmtpConfiguration.LoadSmtpConfigurationAsync();
            
            // Cargar documentos previamente generados
            await DocumentManagement.LoadPreviouslyGeneratedDocuments();
            
            // Sincronizar estados iniciales
            AutomaticEmail.UpdateGeneratedDocuments(DocumentManagement.GeneratedDocuments);
            AutomaticEmail.UpdateEmailConfiguration(SmtpConfiguration.IsEmailConfigured);
            
            GlobalStatusMessage = "Componentes inicializados correctamente";
            _logger.LogInformation("✅ MainDocumentGenerationViewModel inicializado completamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inicializando MainDocumentGenerationViewModel");
            GlobalStatusMessage = "Error en la inicialización";
        }
    }

    /// <summary>
    /// Limpia recursos y se desuscribe de eventos
    /// </summary>
    public void Cleanup()
    {
        try
        {
            _configurationService.ConfigurationChanged -= OnConfigurationChanged;
            
            SmtpConfiguration?.Cleanup();
            AutomaticEmail?.Cleanup();
            
            _logger.LogInformation("🧹 MainDocumentGenerationViewModel limpiado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la limpieza");
        }
    }
}
