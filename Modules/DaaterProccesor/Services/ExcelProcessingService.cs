using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.DaaterProccesor.Services;

public class ExcelProcessingService : IExcelProcessingService
{
    private readonly IResourceLoaderService _resourceLoader;
    private readonly IDataConsolidationService _dataConsolidation;
    private readonly IExcelExportService _excelExport;

    public ExcelProcessingService(
        IResourceLoaderService resourceLoader,
        IDataConsolidationService dataConsolidation,
        IExcelExportService excelExport)
    {
        _resourceLoader = resourceLoader;
        _dataConsolidation = dataConsolidation;
        _excelExport = excelExport;
    }

    public async Task<DataTable> ProcesarArchivosExcelAsync(string folderPath, System.IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var paises = _resourceLoader.LoadPaises();
            cancellationToken.ThrowIfCancellationRequested();
            var partidas = _resourceLoader.LoadPartidas();
            cancellationToken.ThrowIfCancellationRequested();
            var proveedores = _resourceLoader.LoadProveedores();
            cancellationToken.ThrowIfCancellationRequested();
            return _dataConsolidation.ConsolidarDatos(folderPath, paises, partidas, proveedores, progress, cancellationToken);
        }, cancellationToken);
    }

    public async Task GenerarArchivoConsolidadoAsync(DataTable sortedData, string outputFilePath, CancellationToken cancellationToken = default)
    {
        await _excelExport.ExportarConsolidadoAsync(sortedData, outputFilePath, cancellationToken);
    }
}
