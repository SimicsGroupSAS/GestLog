using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionCartera.Models;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.ViewModels.Base;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel especializado en la gestión y carga de documentos generados
/// </summary>
public partial class DocumentManagementViewModel : BaseDocumentGenerationViewModel
{
    [ObservableProperty] private string _outputFolderPath = string.Empty;
    [ObservableProperty] private IReadOnlyList<GeneratedPdfInfo> _generatedDocuments = new List<GeneratedPdfInfo>();

    public DocumentManagementViewModel(IGestLogLogger logger) : base(logger)
    {
        InitializeOutputPath();
        
        // Cargar documentos previamente generados si existen
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadPreviouslyGeneratedDocuments();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar documentos previos en el constructor");
            }
        });
    }

    private void InitializeOutputPath()
    {
        try
        {
            OutputFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archivos", "Clientes cartera pdf");
            
            if (!Directory.Exists(OutputFolderPath))
            {
                Directory.CreateDirectory(OutputFolderPath);
                _logger.LogInformation("📁 Carpeta de documentos creada: {Path}", OutputFolderPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inicializando carpeta de documentos");
        }
    }

    /// <summary>
    /// Carga documentos previamente generados desde el archivo de texto
    /// </summary>
    public async Task LoadPreviouslyGeneratedDocuments()
    {
        try
        {
            var textFilePath = Path.Combine(OutputFolderPath, "pdfs_generados.txt");
            
            if (!File.Exists(textFilePath))
            {
                _logger.LogInformation("ℹ️ No se encontró archivo de documentos previos: {Path}", textFilePath);
                return;
            }

            _logger.LogInformation("🔄 Cargando documentos previamente generados...");
            
            var documents = await LoadDocumentsFromTextFileAsync(textFilePath);
            
            if (documents.Count > 0)
            {
                GeneratedDocuments = documents;
                TotalDocuments = documents.Count;
                IsProcessingCompleted = true;
                
                _logger.LogInformation("✅ Documentos previos cargados: {Count} documentos", documents.Count);
                StatusMessage = $"Documentos previos cargados: {documents.Count} archivos";
            }
            else
            {
                _logger.LogWarning("⚠️ No se pudieron cargar documentos del archivo");
                StatusMessage = "Error al cargar documentos previos";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar documentos previamente generados");
            StatusMessage = "Error al cargar documentos previos";
        }
    }

    /// <summary>
    /// Carga documentos desde el archivo de texto generado
    /// </summary>
    private async Task<List<GeneratedPdfInfo>> LoadDocumentsFromTextFileAsync(string textFilePath)
    {
        var documents = new List<GeneratedPdfInfo>();
        
        try
        {
            _logger.LogInformation("📖 Leyendo archivo de documentos generados: {FilePath}", textFilePath);
            
            var lines = await File.ReadAllLinesAsync(textFilePath);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("=") || line.StartsWith("PDF") || line.StartsWith("Total"))
                    continue;
                
                try
                {
                    // Formato esperado: "NombreArchivo.pdf - Empresa: NOMBRE_EMPRESA - NIT: 123456789"
                    var parts = line.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length >= 3)
                    {
                        var fileName = parts[0].Trim();
                        var empresaPart = parts[1].Replace("Empresa: ", "").Trim();
                        var nitPart = parts[2].Replace("NIT: ", "").Trim();
                        
                        // Construir la ruta completa del archivo
                        var fullPath = Path.Combine(OutputFolderPath, fileName);
                        
                        // Verificar que el archivo existe físicamente
                        if (File.Exists(fullPath))
                        {
                            var document = new GeneratedPdfInfo
                            {
                                NombreArchivo = fileName,
                                NombreEmpresa = empresaPart,
                                Nit = nitPart,
                                RutaArchivo = fullPath
                            };
                            
                            documents.Add(document);
                            _logger.LogDebug("📄 Documento cargado: {Archivo} - {Empresa} (NIT: {Nit})", 
                                fileName, empresaPart, nitPart);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Archivo no encontrado: {FilePath}", fullPath);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Formato de línea inválido: {Line}", line);
                    }
                }
                catch (Exception lineEx)
                {
                    _logger.LogWarning(lineEx, "⚠️ Error procesando línea: {Line}", line);
                }
            }
            
            _logger.LogInformation("✅ Documentos cargados desde archivo: {Count} documentos", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error leyendo archivo de documentos generados");
            throw;
        }
        
        return documents;
    }

    /// <summary>
    /// Actualiza la lista de documentos generados
    /// </summary>
    public void UpdateGeneratedDocuments(IReadOnlyList<GeneratedPdfInfo> documents)
    {
        GeneratedDocuments = documents;
        TotalDocuments = documents.Count;
        
        if (documents.Count > 0)
        {
            IsProcessingCompleted = true;
            StatusMessage = $"Documentos actualizados: {documents.Count} archivos";
        }
        
        _logger.LogInformation("📋 Lista de documentos actualizada: {Count} documentos", documents.Count);
    }

    /// <summary>
    /// Limpia la lista de documentos generados
    /// </summary>
    [RelayCommand]
    public void ClearGeneratedDocuments()
    {
        GeneratedDocuments = new List<GeneratedPdfInfo>();
        TotalDocuments = 0;
        CurrentDocument = 0;
        IsProcessingCompleted = false;
        ProgressValue = 0;
        
        StatusMessage = "Lista de documentos limpiada";
        _logger.LogInformation("🧹 Lista de documentos generados limpiada");
    }

    /// <summary>
    /// Refresca la lista cargando nuevamente desde el archivo
    /// </summary>
    [RelayCommand]
    public async Task RefreshDocuments()
    {
        try
        {
            StatusMessage = "Refrescando lista de documentos...";
            await LoadPreviouslyGeneratedDocuments();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al refrescar documentos");
            StatusMessage = "Error al refrescar documentos";
        }
    }

    /// <summary>
    /// Verifica si existen documentos generados
    /// </summary>
    public bool HasGeneratedDocuments => GeneratedDocuments.Count > 0;

    /// <summary>
    /// Obtiene la cantidad de documentos generados
    /// </summary>
    public int DocumentCount => GeneratedDocuments.Count;
}
