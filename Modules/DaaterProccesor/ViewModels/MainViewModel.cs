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

namespace GestLog.Modules.DaaterProccesor.ViewModels;

public partial class MainViewModel : ObservableObject
{    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string executablePath;

    [ObservableProperty]
    private bool isProcessing;

    [ObservableProperty]
    private string? statusMessage;

    public string LogoPath { get; }

    private readonly IExcelProcessingService _excelService;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainViewModel() : this(
        new ExcelProcessingService(
            new ResourceLoaderService(),
            new DataConsolidationService(),
            new ExcelExportService())) {}

    public MainViewModel(IExcelProcessingService excelService)
    {
        _excelService = excelService;

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
        }        // Asignar la ruta del directorio real al ExecutablePath para mostrarla en la interfaz
        ExecutablePath = directorioReal;
        StatusMessage = "Listo para procesar archivos.";
    }    [RelayCommand(CanExecute = nameof(CanProcessExcelFiles))]
    public async Task ProcessExcelFilesAsync()
    {
        if (IsProcessing) return;
        
        IsProcessing = true;
        StatusMessage = "Iniciando procesamiento...";
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Notificar cambios en los comandos
        ProcessExcelFilesCommand.NotifyCanExecuteChanged();
        CancelProcessingCommand.NotifyCanExecuteChanged();
        
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
                    StatusMessage = "Procesando archivos Excel...";
                    var progress = new System.Progress<double>(p => 
                    {
                        Progress = p;
                        StatusMessage = $"Procesando archivos... {p:F1}%";
                    });
                      // Procesamiento asíncrono de archivos Excel
                    var resultado = await _excelService.ProcesarArchivosExcelAsync(folderPath, progress, _cancellationTokenSource.Token);
                    
                    // Verificar cancelación inmediatamente después del procesamiento
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    
                    StatusMessage = "Generando archivo consolidado...";
                    
                    // Preparar carpeta de salida
                    string rutaReal = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("No se pudo obtener la ruta del ejecutable.");
                    var directorioReal = Path.GetDirectoryName(rutaReal);
                    var outputFolder = Path.Combine(directorioReal!, "Output");
                    if (!Directory.Exists(outputFolder))
                        Directory.CreateDirectory(outputFolder);
                    
                    var outputFilePath = Path.Combine(outputFolder, "Consolidado.xlsx");
                      // Generar archivo consolidado de forma asíncrona
                    await _excelService.GenerarArchivoConsolidadoAsync(resultado, outputFilePath, _cancellationTokenSource.Token);
                    
                    // Verificar cancelación después de la generación
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    
                    StatusMessage = "Procesamiento completado exitosamente.";
                    MessageBox.Show($"Archivo consolidado generado exitosamente en: {outputFilePath}", "Éxito");
                }
            }
            else
            {
                StatusMessage = "Operación cancelada por el usuario.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operación cancelada.";
            MessageBox.Show("Operación cancelada por el usuario.", "Cancelado");
        }
        catch (Exception ex)
        {
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
        }
    }    [RelayCommand(CanExecute = nameof(CanCancelProcessing))]
    public void CancelProcessing()
    {
        System.Diagnostics.Debug.WriteLine("CancelProcessing ejecutado");
        System.Diagnostics.Debug.WriteLine($"Estado antes de cancelar: IsProcessing={IsProcessing}, TokenSource={_cancellationTokenSource != null}");
        
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelando operación...";
        
        System.Diagnostics.Debug.WriteLine("Token de cancelación activado");
    }

    private bool CanProcessExcelFiles() => !IsProcessing;

    private bool CanCancelProcessing() 
    {
        var canCancel = IsProcessing && _cancellationTokenSource != null;
        System.Diagnostics.Debug.WriteLine($"CanCancelProcessing: IsProcessing={IsProcessing}, CancellationTokenSource={_cancellationTokenSource != null}, Result={canCancel}");
        return canCancel;
    }
}