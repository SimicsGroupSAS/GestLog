using System.Data;

namespace GestLog.Modules.DaaterProccesor.Services;

public interface IExcelExportService
{
    void ExportarConsolidado(DataTable sortedData, string outputFilePath);
}
