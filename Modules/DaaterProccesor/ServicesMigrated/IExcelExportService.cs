using System.Data;

namespace GestLog.ServicesMigrated;

public interface IExcelExportService
{
    void ExportarConsolidado(DataTable sortedData, string outputFilePath);
}
