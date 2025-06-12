using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.UI;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.Models;
using GestLog.Modules.GestionCartera.ViewModels.Base;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel especializado en la generación de documentos PDF
/// </summary>
public partial class PdfGenerationViewModel : BaseDocumentGenerationViewModel
{
    private readonly IPdfGeneratorService _pdfGenerator;
    private const string DEFAULT_TEMPLATE_FILE = "PlantillaSIMICS.png";

    // Servicio de progreso suavizado para animación fluida
    private SmoothProgressService _smoothProgress = null!; // Será inicializado en el constructor

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(GenerateDocumentsCommand))]
    private string _selectedExcelFilePath = string.Empty;
    
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(GenerateDocumentsCommand))]
    private string _outputFolderPath = string.Empty;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(TemplateStatusMessage))]
    private string _templateFilePath = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemplateStatusMessage))]
    private bool _useDefaultTemplate = true;    [ObservableProperty] private IReadOnlyList<GeneratedPdfInfo> _generatedDocuments = new List<GeneratedPdfInfo>();
    [ObservableProperty] private string _logText = string.Empty;
    
    // Propiedades para el panel de finalización
    [ObservableProperty] private bool _showCompletionPanel = false;
    [ObservableProperty] private string _completionMessage = string.Empty;

    public string TemplateStatusMessage => GetTemplateStatusMessage();public PdfGenerationViewModel(IPdfGeneratorService pdfGenerator, IGestLogLogger logger)
        : base(logger)
    {
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        
        // Inicializar el servicio de progreso suavizado
        _smoothProgress = new SmoothProgressService(value => ProgressValue = value);
        
        InitializeDefaultPaths();
    }

    private string GetTemplateStatusMessage()
    {
        if (!UseDefaultTemplate)
            return "Plantilla desactivada - se usará fondo blanco";
            
        if (string.IsNullOrEmpty(TemplateFilePath))
            return "No se ha encontrado una plantilla";
            
        if (Path.GetFileName(TemplateFilePath) == DEFAULT_TEMPLATE_FILE)
            return $"Usando plantilla predeterminada: {DEFAULT_TEMPLATE_FILE}";
            
        return $"Usando plantilla personalizada: {Path.GetFileName(TemplateFilePath)}";
    }

    private void InitializeDefaultPaths()
    {
        try
        {
            OutputFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archivos", "Clientes cartera pdf");
            
            if (!Directory.Exists(OutputFolderPath))
            {
                Directory.CreateDirectory(OutputFolderPath);
                _logger.LogInformation("📁 Carpeta de salida creada: {Path}", OutputFolderPath);
            }

            // Configurar plantilla por defecto
            var defaultTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", DEFAULT_TEMPLATE_FILE);
            if (File.Exists(defaultTemplatePath))
            {
                TemplateFilePath = defaultTemplatePath;
                _logger.LogInformation("🖼️ Plantilla predeterminada encontrada: {Path}", defaultTemplatePath);
            }
            else
            {
                _logger.LogWarning("⚠️ Plantilla predeterminada no encontrada en: {Path}", defaultTemplatePath);
            }
            
            // Notificar explícitamente que ha cambiado la posibilidad de ejecutar los comandos
            // después de la inicialización
            NotifyCommandsCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inicializando rutas por defecto");
        }
    }

    [RelayCommand]
    private void SelectExcelFile()
    {        
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar archivo Excel",
                Filter = "Archivos Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };            if (openFileDialog.ShowDialog() == true)
            {
                SelectedExcelFilePath = openFileDialog.FileName;
                _logger.LogInformation("📊 Archivo Excel seleccionado: {Path}", SelectedExcelFilePath);
                StatusMessage = $"Archivo Excel seleccionado: {Path.GetFileName(SelectedExcelFilePath)}";
                
                // Asegurar notificación en el hilo de UI
                NotifyCommandsCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al seleccionar archivo Excel");
            StatusMessage = "Error al seleccionar archivo Excel";
        }
    }

    [RelayCommand]
    private void SelectOutputFolder()
    {
        try
        {
            var folderDialog = new VistaFolderBrowserDialog
            {
                Description = "Seleccionar carpeta de salida para los documentos PDF",
                UseDescriptionForTitle = true,
                SelectedPath = OutputFolderPath
            };

            if (folderDialog.ShowDialog() == true)
            {
                OutputFolderPath = folderDialog.SelectedPath;
                _logger.LogInformation("📁 Carpeta de salida seleccionada: {Path}", OutputFolderPath);
                StatusMessage = $"Carpeta de salida: {OutputFolderPath}";
                
                // Notificar explícitamente el cambio para el comando
                NotifyCommandsCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al seleccionar carpeta de salida");
            StatusMessage = "Error al seleccionar carpeta de salida";
        }
    }

    [RelayCommand]
    private void SelectTemplate()
    {        
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar plantilla de imagen",
                Filter = "Archivos de imagen (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TemplateFilePath = openFileDialog.FileName;
                UseDefaultTemplate = false;
                _logger.LogInformation("🖼️ Plantilla personalizada seleccionada: {Path}", TemplateFilePath);
                StatusMessage = $"Plantilla seleccionada: {Path.GetFileName(TemplateFilePath)}";
                OnPropertyChanged(nameof(TemplateStatusMessage));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al seleccionar plantilla");
            StatusMessage = "Error al seleccionar plantilla";
        }
    }

    [RelayCommand]
    private void ClearTemplate()
    {
        UseDefaultTemplate = false;
        _logger.LogInformation("🗑️ Uso de plantilla desactivado");
        StatusMessage = "Plantilla desactivada - se usará fondo blanco";
        OnPropertyChanged(nameof(TemplateStatusMessage));
    }    [RelayCommand(CanExecute = nameof(CanGenerateDocuments))]
    private async Task GenerateDocuments()
    {
        if (IsProcessing) return;

        try
        {
            IsProcessing = true;
            IsProcessingCompleted = false;
            
            // Resetear progreso usando el servicio suavizado
            _smoothProgress.SetValueDirectly(0);
            CurrentDocument = 0;
            TotalDocuments = 0;
            GeneratedDocuments = new List<GeneratedPdfInfo>();

            _cancellationTokenSource = new CancellationTokenSource();
            
            _logger.LogInformation("🚀 Iniciando generación de documentos PDF");
            _logger.LogInformation("📊 Archivo Excel: {ExcelPath}", SelectedExcelFilePath);
            _logger.LogInformation("📁 Carpeta de salida: {OutputPath}", OutputFolderPath);
            _logger.LogInformation("🖼️ Plantilla: {Template}", UseDefaultTemplate ? TemplateFilePath : "Sin plantilla");

            StatusMessage = "Generando documentos PDF...";

            var templateToUse = UseDefaultTemplate ? TemplateFilePath : null;
            var result = await _pdfGenerator.GenerateEstadosCuentaAsync(
                SelectedExcelFilePath,
                OutputFolderPath,
                templateToUse,
                new Progress<(int current, int total, string status)>(OnProgressUpdated),
                _cancellationTokenSource.Token
            );

            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                StatusMessage = "Generación cancelada por el usuario";
                _logger.LogWarning("⚠️ Generación de documentos cancelada por el usuario");
            }            else if (result?.Count > 0)
            {
                GeneratedDocuments = result;
                TotalDocuments = GeneratedDocuments.Count;
                IsProcessingCompleted = true;
                StatusMessage = $"✅ Generación completada: {TotalDocuments} documentos generados";
                _logger.LogInformation("✅ Generación completada exitosamente: {Count} documentos", TotalDocuments);

                // Completar el progreso suavemente al 100%
                _smoothProgress.Report(100);
                await Task.Delay(200); // Pequeña pausa para mostrar la finalización

                // Guardar lista de documentos generados
                await SaveGeneratedDocumentsList();
                
                // Mostrar panel de finalización con mensaje personalizado
                CompletionMessage = $"🎉 ¡Generación completada exitosamente!\n\n" +
                                   $"📊 Documentos generados: {TotalDocuments}\n" +
                                   $"📁 Ubicación: {OutputFolderPath}\n\n" +
                                   $"💡 Siguiente paso: Configure el envío automático de correos para entregar " +
                                   $"los documentos directamente a sus clientes.";
                ShowCompletionPanel = true;
            }
            else
            {
                StatusMessage = "❌ Error en la generación";
                _logger.LogWarning("❌ Error en la generación de documentos");
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Generación cancelada";
            _logger.LogWarning("⚠️ Generación de documentos cancelada");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error inesperado: {ex.Message}";
            _logger.LogError(ex, "❌ Error inesperado durante la generación de documentos");
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenOutputFolder))]
    private void OpenOutputFolder()
    {
        try
        {
            if (Directory.Exists(OutputFolderPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", OutputFolderPath);
                _logger.LogInformation("📂 Carpeta de salida abierta: {Path}", OutputFolderPath);
            }
            else
            {
                StatusMessage = "La carpeta de salida no existe";
                _logger.LogWarning("⚠️ La carpeta de salida no existe: {Path}", OutputFolderPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al abrir carpeta de salida");
            StatusMessage = "Error al abrir carpeta de salida";        }
    }
    
    [RelayCommand(CanExecute = nameof(IsProcessing))]
    private void CancelGeneration()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _logger.LogInformation("🚫 Cancelación de generación solicitada por el usuario");
            StatusMessage = "Cancelando generación...";
        }
        catch (Exception ex)        {
            _logger.LogError(ex, "Error al cancelar generación");
            StatusMessage = "Error al cancelar";
        }
    }
    
    [RelayCommand]
    public void ResetProgressData()
    {
        try
        {
            ResetProgress();
            _logger.LogInformation("🔄 Progreso de generación reiniciado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reiniciar progreso");
            StatusMessage = "Error al reiniciar";
        }
    }
    
    [RelayCommand]
    public void GoToEmailTab()
    {
        try
        {
            _logger.LogInformation("🚀 Usuario navegó a la pestaña de envío de correos");
            
            // Buscar el TabControl en la vista y cambiar a la segunda pestaña (Envío Automático)
            // Este método será llamado desde el XAML y necesita interactuar con la vista
            ShowCompletionPanel = false; // Ocultar el panel de finalización
            
            // Crear mensaje para el log del sistema
            LogText += "\n📧 Navegando a la pestaña de Envío Automático...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al navegar a la pestaña de email");
        }
    }
    
    protected override void ResetProgress()
    {
        base.ResetProgress();
        // Resetear el progreso suavizado también
        _smoothProgress.SetValueDirectly(0);
        // Ocultar panel de finalización
        ShowCompletionPanel = false;
        CompletionMessage = string.Empty;
    }

    public bool CanGenerateDocuments()
    {
        bool isNotProcessing = !IsProcessing;
        bool hasExcelPath = !string.IsNullOrWhiteSpace(SelectedExcelFilePath);
        bool excelExists = FileExistsWithNetworkSupport(SelectedExcelFilePath);
        bool hasOutputPath = !string.IsNullOrWhiteSpace(OutputFolderPath);
        
        return isNotProcessing && hasExcelPath && excelExists && hasOutputPath;
    }

    private bool CanOpenOutputFolder() => 
        !string.IsNullOrWhiteSpace(OutputFolderPath) && 
        Directory.Exists(OutputFolderPath);    private void OnProgressUpdated((int current, int total, string status) progress)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentDocument = progress.current;
            TotalDocuments = progress.total;
            
            // Calcular el progreso y usar el servicio suavizado para animación fluida
            var progressPercentage = progress.total > 0 ? (double)progress.current / progress.total * 100 : 0;
            _smoothProgress.Report(progressPercentage);
            
            StatusMessage = progress.status;
            
            if (!string.IsNullOrEmpty(progress.status))
            {
                LogText += $"{DateTime.Now:HH:mm:ss} - {progress.status}\n";
            }
        });
    }

    private async Task SaveGeneratedDocumentsList()
    {
        try
        {
            var textFilePath = Path.Combine(OutputFolderPath, "pdfs_generados.txt");
            var lines = new List<string>
            {
                $"Fecha de generación: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Total de PDFs generados: {GeneratedDocuments.Count}",
                "-------------------------------------------------------------"
            };

            foreach (var doc in GeneratedDocuments)
            {
                lines.Add($"Empresa: {doc.NombreEmpresa}");
                lines.Add($"NIT: {doc.Nit}");
                lines.Add($"Archivo: {doc.NombreArchivo}");
                lines.Add($"Tipo: {doc.TipoCartera}");
                lines.Add($"Ruta: {doc.RutaArchivo}");
                lines.Add("-------------------------------------------------------------");
            }

            await File.WriteAllLinesAsync(textFilePath, lines);
            _logger.LogInformation("💾 Lista de documentos guardada en: {Path}", textFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error guardando lista de documentos generados");
        }
    }    /// <summary>
    /// Verifica la existencia de un archivo, con manejo específico para rutas de red
    /// </summary>
    private bool FileExistsWithNetworkSupport(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
            
        try
        {
            // Para rutas de red, implementamos un sistema más robusto
            if (filePath.StartsWith(@"\\"))
            {
                // Implementar reintentos para mayor robustez en rutas de red
                int maxRetries = 3;
                int retryDelayMs = 500;
                
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        bool exists = fileInfo.Exists;
                        
                        // Si lo encontramos, devolvemos inmediatamente
                        if (exists) return true;
                        
                        // Si no existe y no es el último intento, esperamos antes de reintentar
                        if (attempt < maxRetries)
                        {
                            Thread.Sleep(retryDelayMs);
                            retryDelayMs *= 2; // Backoff exponencial
                        }
                    }
                    catch (IOException)
                    {
                        if (attempt < maxRetries)
                        {
                            Thread.Sleep(retryDelayMs);
                            retryDelayMs *= 2;
                        }
                    }
                }
                
                return false;
            }
            
            // Ruta local normal
            return File.Exists(filePath);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error verificando existencia del archivo: {FilePath}. Error: {Error}", 
                filePath, ex.Message);
            return false;
        }
    }/// <summary>
    /// Notifica que el estado de los comandos puede haber cambiado, asegurando que la notificación
    /// se ejecuta en el hilo de UI
    /// </summary>
    private void NotifyCommandsCanExecuteChanged()
    {
        // Asegurar que la notificación se ejecuta en el hilo de UI
        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            GenerateDocumentsCommand.NotifyCanExecuteChanged();
        }
        else
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                GenerateDocumentsCommand.NotifyCanExecuteChanged();
            });
        }
    }
}
