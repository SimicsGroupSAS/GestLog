using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Services;

namespace GestLog.Modules.DaaterProccesor.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly IGestLogLogger _logger;    public ExcelExportService(IGestLogLogger logger)
    {
        _logger = logger;
    }

    public async Task ExportarConsolidadoAsync(DataTable sortedData, string outputFilePath, CancellationToken cancellationToken = default)
    {
        await _logger.LoggedOperationAsync("Exportación a Excel", async () =>
        {
            _logger.LogDebug("📁 Iniciando exportación de datos consolidados a: {OutputPath}", outputFilePath);
            _logger.LogDebug("📊 Datos a exportar: {RowCount} filas, {ColumnCount} columnas", 
                sortedData.Rows.Count, sortedData.Columns.Count);

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("📝 Creando workbook de Excel...");
                using var workbook = new XLWorkbook();
                
                // Configurar hoja GenDesc
                _logger.LogDebug("📋 Configurando hoja 'GenDesc'...");
                var genDescWorksheet = workbook.Worksheets.Add("GenDesc");
                genDescWorksheet.Cell(1, 1).InsertTable(sortedData);
                genDescWorksheet.Cells().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                genDescWorksheet.Cells().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                genDescWorksheet.Columns().AdjustToContents();
                genDescWorksheet.Column(1).Style.NumberFormat.Format = "yyyy-MM-dd";
                genDescWorksheet.Column(3).Style.NumberFormat.Format = "0";
                genDescWorksheet.Column(4).Style.NumberFormat.Format = "0";
                genDescWorksheet.Column(12).Style.NumberFormat.Format = "0";
                genDescWorksheet.Column(19).Style.NumberFormat.Format = "0";        
                genDescWorksheet.Column(20).Style.NumberFormat.Format = "0.00";
                genDescWorksheet.Column(21).Style.NumberFormat.Format = "#,##0.00";
                genDescWorksheet.Column(22).Style.NumberFormat.Format = "$#,##0.00";
                genDescWorksheet.Column(23).Width = 40;
                genDescWorksheet.Column(23).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                
                cancellationToken.ThrowIfCancellationRequested();
                  // Configurar hoja SpecProd_Interes
                _logger.LogDebug("📋 Configurando hoja 'SpecProd_Interes'...");
                var specProdWorksheet = workbook.Worksheets.Add("SpecProd_Interes");
                specProdWorksheet.Cell(1, 1).Value = "NUMERO DECLARACION";
                specProdWorksheet.Cell(1, 2).Value = "ESTANDAR";
                specProdWorksheet.Cell(1, 3).Value = "DIM MAIN";
                specProdWorksheet.Cell(1, 4).Value = "OTRAS DIM";
                specProdWorksheet.Cell(1, 5).Value = "UNIDADES";
                specProdWorksheet.Cell(1, 6).Value = "FORMA";
                specProdWorksheet.Cell(1, 7).Value = "CANTIDAD";
                specProdWorksheet.Cell(1, 8).Value = "PESO T";
                specProdWorksheet.Cell(1, 9).Value = "DETALLES STD";
                specProdWorksheet.Cell(1, 10).Value = "MES";
                specProdWorksheet.Row(1).Style.Font.Bold = true;
                specProdWorksheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                specProdWorksheet.Columns().AdjustToContents();
                
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("💾 Guardando archivo: {OutputPath}", outputFilePath);
                workbook.SaveAs(outputFilePath);
                
                _logger.LogDebug("✅ Archivo Excel exportado exitosamente");
            }, cancellationToken);
        });
    }
}
