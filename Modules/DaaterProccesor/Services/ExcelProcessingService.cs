using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Services;
using Microsoft.Extensions.Logging;

namespace GestLog.Modules.DaaterProccesor.Services;

public class ExcelProcessingService : IExcelProcessingService
{
    private readonly IResourceLoaderService _resourceLoader;
    private readonly IDataConsolidationService _dataConsolidation;
    private readonly IExcelExportService _excelExport;
    private readonly IGestLogLogger _logger;

    public ExcelProcessingService(
        IResourceLoaderService resourceLoader,
        IDataConsolidationService dataConsolidation,
        IExcelExportService excelExport,
        IGestLogLogger logger)
    {
        _resourceLoader = resourceLoader;
        _dataConsolidation = dataConsolidation;
        _excelExport = excelExport;
        _logger = logger;
    }

    // Constructor sin logging para compatibilidad hacia atrás
    public ExcelProcessingService(
        IResourceLoaderService resourceLoader,
        IDataConsolidationService dataConsolidation,
        IExcelExportService excelExport)
        : this(resourceLoader, dataConsolidation, excelExport, LoggingService.GetLogger())
    {
    }    public async Task<DataTable> ProcesarArchivosExcelAsync(string folderPath, System.IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.Logger.LogInformation("🔄 Iniciando carga de recursos de referencia");
            
            cancellationToken.ThrowIfCancellationRequested();
            _logger.Logger.LogDebug("📚 Cargando países...");
            var paises = _resourceLoader.LoadPaises();
            _logger.Logger.LogInformation("✅ Países cargados: {Count}", paises.Count);
            
            cancellationToken.ThrowIfCancellationRequested();
            _logger.Logger.LogDebug("📊 Cargando partidas arancelarias...");
            var partidas = _resourceLoader.LoadPartidas();
            _logger.Logger.LogInformation("✅ Partidas cargadas: {Count}", partidas.Count);
            
            cancellationToken.ThrowIfCancellationRequested();
            _logger.Logger.LogDebug("🏢 Cargando proveedores...");
            var proveedores = _resourceLoader.LoadProveedores();
            _logger.Logger.LogInformation("✅ Proveedores cargados: {Count}", proveedores.Count);
            
            cancellationToken.ThrowIfCancellationRequested();
            _logger.Logger.LogInformation("🔗 Iniciando consolidación de datos desde: {FolderPath}", folderPath);
            
            return _dataConsolidation.ConsolidarDatos(folderPath, paises, partidas, proveedores, progress, cancellationToken);
        }, cancellationToken);
    }    public async Task GenerarArchivoConsolidadoAsync(DataTable sortedData, string outputFilePath, CancellationToken cancellationToken = default)
    {
        _logger.Logger.LogInformation("📄 Generando archivo consolidado: {OutputPath} ({RowCount} filas)", 
            outputFilePath, sortedData.Rows.Count);
        
        await _excelExport.ExportarConsolidadoAsync(sortedData, outputFilePath, cancellationToken);
        
        _logger.Logger.LogInformation("✅ Archivo consolidado generado exitosamente: {OutputPath}", outputFilePath);
    }
}
