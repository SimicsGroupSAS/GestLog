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
using GestLog.Modules.GestionCartera.Services;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel para la vista de generación de documentos PDF
/// </summary>
public partial class DocumentGenerationViewModel : ObservableObject
{
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly IGestLogLogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private const string DEFAULT_TEMPLATE_FILE = "PlantillaSIMICS.png";

    [ObservableProperty] private string _selectedExcelFilePath = string.Empty;
    [ObservableProperty] private string _outputFolderPath = string.Empty;
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(TemplateStatusMessage))]
    private string _templateFilePath = string.Empty;
    
    [ObservableProperty] private string _logText = string.Empty;
    [ObservableProperty] private string _statusMessage = "Listo para generar documentos";
    [ObservableProperty] private bool _isProcessing = false;
    [ObservableProperty] private double _progressValue = 0;    [ObservableProperty] private int _totalDocuments = 0;
    [ObservableProperty] private int _currentDocument = 0;
    [ObservableProperty] private IReadOnlyList<GeneratedPdfInfo> _generatedDocuments = new List<GeneratedPdfInfo>();
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemplateStatusMessage))]
    private bool _useDefaultTemplate = true;

    public string TemplateStatusMessage => GetTemplateStatusMessage();

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

    public DocumentGenerationViewModel(IPdfGeneratorService pdfGenerator, IGestLogLogger logger)
    {
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Configurar carpeta de salida por defecto
        InitializeDefaultPaths();
    }    private void InitializeDefaultPaths()
    {
        try
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var archivosFolder = Path.Combine(appDirectory, "Archivos");
            var outputFolder = Path.Combine(archivosFolder, "Clientes cartera pdf");
            
            // Crear carpeta si no existe
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                AddLog("📁 Carpeta de salida creada automáticamente");
            }

            OutputFolderPath = outputFolder;
            // Forzar la notificación de cambio de propiedad para re-evaluar los CanExecute
            OnPropertyChanged(nameof(OutputFolderPath));
            
            // Asegurarse de que existe la carpeta Assets en el directorio de salida
            EnsureAssetsDirectoryExists(appDirectory);
            
            // Configurar plantilla por defecto - buscar en varias ubicaciones posibles
            string defaultTemplatePath = Path.Combine(appDirectory, "Assets", DEFAULT_TEMPLATE_FILE);
            
            // Si no se encuentra en la primera ubicación, intentar en la carpeta superior (raíz del proyecto)
            if (!File.Exists(defaultTemplatePath))
            {
                string projectRoot = Path.GetFullPath(Path.Combine(appDirectory, "..\\..\\..\\"));
                string sourceTemplatePath = Path.Combine(projectRoot, "Assets", DEFAULT_TEMPLATE_FILE);
                
                // Si existe en la raíz del proyecto, copiarlo al directorio de salida
                if (File.Exists(sourceTemplatePath))
                {
                    _logger.LogInformation("Copiando plantilla desde la raíz del proyecto a la carpeta de salida");
                    var outputAssetsDir = Path.Combine(appDirectory, "Assets");
                    Directory.CreateDirectory(outputAssetsDir);
                    
                    // Asegurarse de que el directorio existe
                    if (!Directory.Exists(outputAssetsDir))
                    {
                        Directory.CreateDirectory(outputAssetsDir);
                    }
                    
                    // Copiar el archivo
                    File.Copy(sourceTemplatePath, defaultTemplatePath, true);
                    AddLog($"📋 Plantilla copiada desde la raíz del proyecto");
                }
                else
                {
                    _logger.LogInformation("Buscando plantilla en raíz del proyecto: {TemplatePath}", sourceTemplatePath);
                }
            }
            
            // Verificar de nuevo después de posiblemente copiar el archivo
            if (File.Exists(defaultTemplatePath))
            {
                TemplateFilePath = defaultTemplatePath;
                AddLog($"🎨 Plantilla cargada automáticamente: {DEFAULT_TEMPLATE_FILE}");
            }
            else
            {                _logger.LogWarning("No se encontró la plantilla por defecto en {TemplatePath}", defaultTemplatePath);
                AddLog("⚠️ No se encontró la plantilla por defecto. Se usará fondo blanco.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar rutas por defecto");
            AddLog($"❌ Error al configurar rutas: {ex.Message}");
        }
    }

    /// <summary>
    /// Asegura que el directorio Assets existe en la carpeta de salida y contiene los archivos necesarios
    /// </summary>
    private void EnsureAssetsDirectoryExists(string appDirectory)
    {
        try
        {
            var outputAssetsDir = Path.Combine(appDirectory, "Assets");
            
            // Crear el directorio si no existe
            if (!Directory.Exists(outputAssetsDir))
            {
                Directory.CreateDirectory(outputAssetsDir);
                _logger.LogInformation("Carpeta Assets creada en el directorio de salida");
                
                // Intentar copiar archivos desde la raíz del proyecto
                string projectRoot = Path.GetFullPath(Path.Combine(appDirectory, "..\\..\\..\\"));
                string sourceAssetsDir = Path.Combine(projectRoot, "Assets");
                
                if (Directory.Exists(sourceAssetsDir))
                {
                    _logger.LogInformation("Copiando archivos de Assets desde la raíz del proyecto");
                    
                    // Copiar PlantillaSIMICS.png y firma.png
                    string[] filesToCopy = { DEFAULT_TEMPLATE_FILE, "firma.png" };
                    
                    foreach (string file in filesToCopy)
                    {
                        string sourcePath = Path.Combine(sourceAssetsDir, file);
                        string destPath = Path.Combine(outputAssetsDir, file);
                        
                        if (File.Exists(sourcePath))
                        {
                            File.Copy(sourcePath, destPath, true);
                            AddLog($"📋 Archivo {file} copiado a la carpeta Assets del directorio de salida");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asegurar el directorio Assets");
            AddLog($"❌ Error al configurar carpeta Assets: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectExcelFile))]
    private async Task SelectExcelFile()
    {        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar archivo Excel",
                Filter = "Archivos Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {                SelectedExcelFilePath = dialog.FileName;
                AddLog($"📄 Archivo seleccionado: {Path.GetFileName(SelectedExcelFilePath)}");
                
                // Forzar la notificación de cambio de propiedad para re-evaluar los CanExecute
                OnPropertyChanged(nameof(SelectedExcelFilePath));
                GenerateDocumentsCommand.NotifyCanExecuteChanged();
                
                // Validar estructura del archivo
                StatusMessage = "Validando archivo Excel...";
                var isValid = await _pdfGenerator.ValidateExcelStructureAsync(SelectedExcelFilePath);
                
                if (isValid)
                {
                    // Obtener vista previa de empresas
                    var companies = await _pdfGenerator.GetCompaniesPreviewAsync(SelectedExcelFilePath);
                    var companiesList = companies.ToList();
                    
                    AddLog($"✅ Archivo válido. Se encontraron {companiesList.Count} empresas");
                    if (companiesList.Count > 0)
                    {
                        AddLog($"📊 Empresas encontradas: {string.Join(", ", companiesList.Take(5))}" + 
                               (companiesList.Count > 5 ? "..." : ""));
                    }
                    StatusMessage = $"Archivo válido - {companiesList.Count} empresas encontradas";
                }
                else
                {
                    AddLog("❌ El archivo Excel no tiene la estructura esperada");
                    StatusMessage = "Error: Archivo Excel inválido";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar archivo Excel");
            AddLog($"❌ Error al seleccionar archivo: {ex.Message}");
            StatusMessage = "Error al seleccionar archivo";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectOutputFolder))]
    private void SelectOutputFolder()
    {
        try
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Seleccionar carpeta de destino para los PDFs",
                UseDescriptionForTitle = true,
                SelectedPath = OutputFolderPath
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFolderPath = dialog.SelectedPath;
                AddLog($"📁 Carpeta de destino: {OutputFolderPath}");
                StatusMessage = "Carpeta de destino actualizada";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar carpeta de destino");
            AddLog($"❌ Error al seleccionar carpeta: {ex.Message}");
        }
    }    [RelayCommand(CanExecute = nameof(CanSelectTemplateFile))]
    private void SelectTemplateFile()
    {        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar plantilla de fondo personalizada",
                Filter = "Imágenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {
                TemplateFilePath = dialog.FileName;
                UseDefaultTemplate = true;
                AddLog($"🎨 Plantilla personalizada seleccionada: {Path.GetFileName(TemplateFilePath)}");
                StatusMessage = "Plantilla de fondo configurada";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar plantilla");
            AddLog($"❌ Error al seleccionar plantilla: {ex.Message}");
        }
    }    [RelayCommand]
    private void ClearTemplate()
    {
        UseDefaultTemplate = false;
        AddLog("🗑️ Uso de plantilla desactivado");
        StatusMessage = "Plantilla desactivada - se usará fondo blanco";
        OnPropertyChanged(nameof(TemplateStatusMessage));
    }

    [RelayCommand(CanExecute = nameof(CanGenerateDocuments))]
    private async Task GenerateDocuments()
    {
        if (IsProcessing) return;

        try
        {
            IsProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            GeneratedDocuments = new List<GeneratedPdfInfo>();
            
            AddLog("🚀 Iniciando generación de documentos PDF...");
            StatusMessage = "Generando documentos...";
            
            var progress = new Progress<(int current, int total, string status)>(report =>
            {
                CurrentDocument = report.current;
                TotalDocuments = report.total;
                if (report.total > 0)
                {
                    ProgressValue = (double)report.current / report.total * 100;
                }
                StatusMessage = report.status;
                AddLog($"📝 {report.status} ({report.current}/{report.total})");            });

            // Determinar si se debe usar la plantilla
            string? templatePath = null;
            if (UseDefaultTemplate && !string.IsNullOrEmpty(TemplateFilePath)) 
            {
                templatePath = TemplateFilePath;
                AddLog($"🎨 Usando plantilla: {Path.GetFileName(TemplateFilePath)}");
            }
            else
            {
                AddLog("⚪ Generando documentos sin plantilla de fondo");
            }
            
            var result = await _pdfGenerator.GenerateEstadosCuentaAsync(
                SelectedExcelFilePath,
                OutputFolderPath,
                templatePath,
                progress,
                _cancellationTokenSource.Token);

            GeneratedDocuments = result;
              AddLog($"🎉 Generación completada exitosamente!");
            AddLog($"📊 Documentos generados: {result.Count}");
            
            if (result.Count > 0)
            {
                var totalSize = result.Sum(d => 
                {
                    try
                    {
                        var fileInfo = new FileInfo(d.RutaArchivo);
                        return fileInfo.Exists ? fileInfo.Length : 0;
                    }
                    catch
                    {
                        return 0;
                    }
                });
                AddLog($"💾 Tamaño total: {FormatFileSize(totalSize)}");
                AddLog($"📁 Ubicación: {OutputFolderPath}");
            }
            
            StatusMessage = $"Completado - {result.Count} documentos generados";
            ProgressValue = 100;
        }
        catch (OperationCanceledException)
        {
            AddLog("⏹️ Generación cancelada por el usuario");
            StatusMessage = "Generación cancelada";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la generación de documentos");
            AddLog($"❌ Error durante la generación: {ex.Message}");
            StatusMessage = "Error durante la generación";
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelGeneration))]
    private void CancelGeneration()
    {
        _cancellationTokenSource?.Cancel();
        AddLog("⏹️ Solicitando cancelación...");
        StatusMessage = "Cancelando...";
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogText = string.Empty;
        StatusMessage = "Log limpiado";
    }

    [RelayCommand(CanExecute = nameof(CanOpenOutputFolder))]
    private void OpenOutputFolder()
    {
        try
        {
            if (Directory.Exists(OutputFolderPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", OutputFolderPath);
                AddLog($"📂 Abriendo carpeta: {OutputFolderPath}");
            }
            else
            {
                AddLog("❌ La carpeta de destino no existe");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al abrir carpeta de destino");
            AddLog($"❌ Error al abrir carpeta: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowDebugInfo()
    {
        try
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(appDirectory, "..\\..\\..\\"));
            
            // Verificar rutas de búsqueda de la plantilla
            var possiblePaths = new List<string>
            {
                Path.Combine(appDirectory, "Assets", DEFAULT_TEMPLATE_FILE),
                Path.Combine(projectRoot, "Assets", DEFAULT_TEMPLATE_FILE),
                Path.Combine(Environment.CurrentDirectory, "Assets", DEFAULT_TEMPLATE_FILE)
            };
            
            AddLog("\n🔍 INFORMACIÓN DE DEPURACIÓN:");
            AddLog($"📂 Directorio de la aplicación: {appDirectory}");
            AddLog($"📂 Directorio raíz del proyecto: {projectRoot}");
            AddLog($"📂 Directorio actual: {Environment.CurrentDirectory}");
            
            AddLog("\n🔎 BÚSQUEDA DE PLANTILLA:");
            foreach (var path in possiblePaths)
            {
                bool exists = File.Exists(path);
                AddLog($"  - {path}: {(exists ? "✅ ENCONTRADO" : "❌ NO EXISTE")}");
            }
            
            // Verificar carpeta Assets en la salida
            var outputAssetsDir = Path.Combine(appDirectory, "Assets");
            bool assetsExists = Directory.Exists(outputAssetsDir);
            AddLog($"\n📁 Carpeta Assets en directorio de salida: {(assetsExists ? "✅ EXISTE" : "❌ NO EXISTE")}");
            
            if (assetsExists)
            {
                var files = Directory.GetFiles(outputAssetsDir);
                AddLog($"   Archivos en Assets ({files.Length}):");
                foreach (var file in files)
                {
                    AddLog($"   - {Path.GetFileName(file)}");
                }
            }
            
            // Estado actual
            AddLog($"\n📄 ESTADO ACTUAL:");
            AddLog($"   Plantilla actual: {(string.IsNullOrEmpty(TemplateFilePath) ? "No configurada" : TemplateFilePath)}");
            AddLog($"   Usar plantilla predeterminada: {UseDefaultTemplate}");
            AddLog($"   {TemplateStatusMessage}");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al mostrar información de depuración");
            AddLog($"❌ Error al generar información de depuración: {ex.Message}");
        }
    }    // Métodos CanExecute
    private bool CanSelectExcelFile() => !IsProcessing;
    private bool CanSelectOutputFolder() => !IsProcessing;
    private bool CanSelectTemplateFile() => !IsProcessing;
    private bool CanGenerateDocuments() => !IsProcessing && !string.IsNullOrEmpty(SelectedExcelFilePath) && Directory.Exists(OutputFolderPath);
    private bool CanCancelGeneration() => IsProcessing;
    private bool CanOpenOutputFolder() => !string.IsNullOrEmpty(OutputFolderPath);

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogText += $"[{timestamp}] {message}\n";
        
        // Limitar el log a las últimas 1000 líneas para evitar problemas de memoria
        var lines = LogText.Split('\n');
        if (lines.Length > 1000)
        {
            LogText = string.Join("\n", lines.Skip(lines.Length - 1000));
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
