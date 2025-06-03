using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.DaaterProccesor.Services;

public interface IExcelExportService
{
    Task ExportarConsolidadoAsync(DataTable sortedData, string outputFilePath, CancellationToken cancellationToken = default);
}
