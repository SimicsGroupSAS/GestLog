using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClosedXML.Excel;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using GestLog.Modules.DaaterProccesor.Services;
using GestLog.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.DaaterProccesor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string executablePath = string.Empty;

    [ObservableProperty]
    private bool isProcessing;

    [ObservableProperty]
    private string? statusMessage;

    public string LogoPath { get; private set; } = string.Empty;

    private readonly IExcelProcessingService _excelService;
    private readonly IGestLogLogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    // Constructor para usar desde DI
    public MainViewModel() 
    {
        // Obtener servicios del contenedor de DI
        var serviceProvider = LoggingService.GetServiceProvider();
        _excelService = serviceProvider.GetRequiredService<IExcelProcessingService>();
        _logger = serviceProvider.GetRequiredService<IGestLogLogger>();
        
        // Inicializar propiedades comunes
        InitializeViewModel();
    }
    
    // Constructor para pruebas o instanciación manual
    public MainViewModel(IExcelProcessingService excelService, IGestLogLogger logger)
    {
        _excelService = excelService;
        _logger = logger;
        
        // Inicializar propiedades comunes
        InitializeViewModel();
    }
    
    // Método común de inicialización
    private void InitializeViewModel()
    {
        // Ruta base del proyecto
        var basePath = AppDomain.CurrentDomain.BaseDirectory;

        // Ruta del logo
        LogoPath = Path.Combine(basePath, "Assets", "logo.png");

        // Ruta real del ejecutable
        string rutaReal = Process.GetCurrentProcess().MainModule?.FileName 
            ?? throw new InvalidOperationException("No se pudo obtener la ruta del ejecutable.");
        var directorioReal = Path.GetDirectoryName(rutaReal);
        if (directorioReal == null)
        {
            throw new InvalidOperationException("No se pudo obtener el directorio real del ejecutable.");
        }

        // Crear la carpeta Output en la ruta real del ejecutable
        var outputFolder = Path.Combine(directorioReal, "Output");
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Asignar la ruta del directorio real al ExecutablePath para mostrarla en la interfaz
        ExecutablePath = directorioReal;
        StatusMessage = "Listo para procesar archivos.";
    }

    [RelayCommand(CanExecute = nameof(CanProcessExcelFiles))]
    public async Task ProcessExcelFilesAsync()
    {
        if (IsProcessing) return;
        
        var stopwatch = Stopwatch.StartNew();
        IsProcessing = true;
        StatusMessage = "Iniciando procesamiento...";
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Notificar cambios en los comandos
        ProcessExcelFilesCommand.NotifyCanExecuteChanged();
        CancelProcessingCommand.NotifyCanExecuteChanged();
        
        _logger.Logger.LogInformation("🎯 Usuario inició procesamiento de archivos Excel");
        
        try
        {
            var folderDialog = new VistaFolderBrowserDialog
            {
                Description = "Selecciona una carpeta que contenga archivos Excel",
                UseDescriptionForTitle = true
            };

            if (folderDialog.ShowDialog() == true)
            {
                var folderPath = folderDialog.SelectedPath;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    // Contar archivos Excel en la carpeta
                    var excelFiles = Directory.GetFiles(folderPath, "*.xlsx", SearchOption.AllDirectories)
                        .Where(f => !Path.GetFileName(f).StartsWith("~$")).ToArray();
                    
                    _logger.LogExcelProcessingStarted(folderPath, excelFiles.Length);
                    
                    StatusMessage = "Procesando archivos Excel...";
                    var progress = new System.Progress<double>(p => 
                    {
                        Progress = p;
                        StatusMessage = $"Procesando archivos... {p:F1}%";
                        _logger.Logger.LogDebug("📊 Progreso de procesamiento: {Progress:F1}%", p);
                    });

                    // Procesamiento asíncrono de archivos Excel con logging
                    var resultado = await _logger.LoggedOperationAsync("ProcesarArchivosExcel", 
                        () => _excelService.ProcesarArchivosExcelAsync(folderPath, progress, _cancellationTokenSource.Token),
                        new Dictionary<string, object>
                        {
                            ["FolderPath"] = folderPath,
                            ["FileCount"] = excelFiles.Length
                        });
                    
                    // Verificar cancelación inmediatamente después del procesamiento
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    
                    StatusMessage = "Generando archivo consolidado...";
                    _logger.Logger.LogInformation("📝 Generando archivo consolidado con {RowCount} filas", resultado.Rows.Count);
                    
                    // Preparar carpeta de salida
                    string rutaReal = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("No se pudo obtener la ruta del ejecutable.");
                    var directorioReal = Path.GetDirectoryName(rutaReal);
                    var outputFolder = Path.Combine(directorioReal!, "Output");
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                        _logger.Logger.LogDebug("📁 Carpeta de salida creada: {OutputFolder}", outputFolder);
                    }
                    
                    var outputFilePath = Path.Combine(outputFolder, "Consolidado.xlsx");
                    
                    // Generar archivo consolidado de forma asíncrona con logging
                    await _logger.LoggedOperationAsync("GenerarArchivoConsolidado",
                        () => _excelService.GenerarArchivoConsolidadoAsync(resultado, outputFilePath, _cancellationTokenSource.Token),
                        new Dictionary<string, object>
                        {
                            ["OutputPath"] = outputFilePath,
                            ["RowCount"] = resultado.Rows.Count
                        });
                    
                    // Verificar cancelación después de la generación
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    
                    stopwatch.Stop();
                    _logger.LogExcelProcessingCompleted(outputFilePath, stopwatch.Elapsed, excelFiles.Length);
                    
                    StatusMessage = "Procesamiento completado exitosamente.";
                    MessageBox.Show($"Archivo consolidado generado exitosamente en: {outputFilePath}", "Éxito");
                }
            }
            else
            {
                _logger.Logger.LogInformation("👤 Usuario canceló la selección de carpeta");
                StatusMessage = "Operación cancelada por el usuario.";
            }
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogOperationCancelled("ProcessExcelFiles", $"Tiempo transcurrido: {stopwatch.Elapsed:mm\\:ss}");
            StatusMessage = "Operación cancelada.";
            MessageBox.Show("Operación cancelada por el usuario.", "Cancelado");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogExcelProcessingError("ProcessExcelFilesAsync", ex);
            _logger.LogPerformance("ProcessExcelFiles_Error", stopwatch.Elapsed);
            
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}", "Error");
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            ProcessExcelFilesCommand.NotifyCanExecuteChanged();
            CancelProcessingCommand.NotifyCanExecuteChanged();
            
            if (string.IsNullOrEmpty(StatusMessage) || StatusMessage.Contains("Error"))
                StatusMessage = "Listo para procesar archivos.";
                
            _logger.Logger.LogDebug("🏁 Finalizando ProcessExcelFilesAsync después de {Duration:mm\\:ss}", stopwatch.Elapsed);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelProcessing))]
    public void CancelProcessing()
    {
        _logger.Logger.LogInformation("⏹️ Usuario solicitó cancelación de operación");
        System.Diagnostics.Debug.WriteLine("CancelProcessing ejecutado");
        System.Diagnostics.Debug.WriteLine($"Estado antes de cancelar: IsProcessing={IsProcessing}, TokenSource={_cancellationTokenSource != null}");
        
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelando operación...";
        
        System.Diagnostics.Debug.WriteLine("Token de cancelación activado");
        _logger.Logger.LogDebug("🔄 Token de cancelación activado");
    }

    private bool CanProcessExcelFiles() => !IsProcessing;

    private bool CanCancelProcessing() 
    {
        var canCancel = IsProcessing && _cancellationTokenSource != null;
        System.Diagnostics.Debug.WriteLine($"CanCancelProcessing: IsProcessing={IsProcessing}, CancellationTokenSource={_cancellationTokenSource != null}, Result={canCancel}");
        return canCancel;
    }
}
