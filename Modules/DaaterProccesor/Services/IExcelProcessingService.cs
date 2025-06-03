using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.DaaterProccesor.Services;

public interface IExcelProcessingService
{
    Task<DataTable> ProcesarArchivosExcelAsync(string folderPath, System.IProgress<double> progress, CancellationToken cancellationToken = default);
    Task GenerarArchivoConsolidadoAsync(DataTable sortedData, string outputFilePath, CancellationToken cancellationToken = default);
}
