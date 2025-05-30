using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ClosedXML.Excel;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using GestLog.ServicesMigrated;

namespace GestLog.ViewModelsMigrated;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string executablePath;

    [ObservableProperty]
    private bool isProcessing;

    public string LogoPath { get; }

    private readonly IExcelProcessingService _excelService;

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
        }

        // Asignar la ruta del directorio real al ExecutablePath para mostrarla en la interfaz
        ExecutablePath = directorioReal;
    }

    [RelayCommand(CanExecute = nameof(CanProcessExcelFiles))]
    public async Task ProcessExcelFilesAsync()
    {
        if (IsProcessing) return;
        IsProcessing = true;
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
                    var progress = new System.Progress<double>(p => Progress = p);
                    var resultado = await Task.Run(() => _excelService.ProcesarArchivosExcelAsync(folderPath, progress));
                    string rutaReal = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("No se pudo obtener la ruta del ejecutable.");
                    var directorioReal = Path.GetDirectoryName(rutaReal);
                    var outputFolder = Path.Combine(directorioReal!, "Output");
                    if (!Directory.Exists(outputFolder))
                        Directory.CreateDirectory(outputFolder);
                    var outputFilePath = Path.Combine(outputFolder, "Consolidado.xlsx");
                    _excelService.GenerarArchivoConsolidado(resultado, outputFilePath);
                    MessageBox.Show($"Archivo consolidado generado exitosamente en: {outputFilePath}", "Éxito");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}", "Error");
        }
        finally
        {
            IsProcessing = false;
            ProcessExcelFilesCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanProcessExcelFiles() => !IsProcessing;
}