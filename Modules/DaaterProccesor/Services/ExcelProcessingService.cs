using System;
using System.Data;
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

    public async Task<DataTable> ProcesarArchivosExcelAsync(string folderPath, System.IProgress<double> progress)
    {
        return await Task.Run(() =>
        {
            var paises = _resourceLoader.LoadPaises();
            var partidas = _resourceLoader.LoadPartidas();
            var proveedores = _resourceLoader.LoadProveedores();
            return _dataConsolidation.ConsolidarDatos(folderPath, paises, partidas, proveedores, progress);
        });
    }

    public void GenerarArchivoConsolidado(DataTable sortedData, string outputFilePath)
    {
        _excelExport.ExportarConsolidado(sortedData, outputFilePath);
    }
}
