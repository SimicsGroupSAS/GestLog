using System.Data;
using System.Threading.Tasks;

namespace GestLog.ServicesMigrated;

public interface IExcelProcessingService
{
    Task<DataTable> ProcesarArchivosExcelAsync(string folderPath, System.IProgress<double> progress);
    void GenerarArchivoConsolidado(DataTable sortedData, string outputFilePath);
}
