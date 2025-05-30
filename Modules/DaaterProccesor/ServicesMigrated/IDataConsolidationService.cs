using System.Collections.Generic;
using System.Data;

namespace GestLog.ServicesMigrated;

public interface IDataConsolidationService
{
    DataTable ConsolidarDatos(
        string folderPath,
        Dictionary<string, string> paises,
        Dictionary<long, string[]> partidas,
        Dictionary<string, string> proveedores,
        System.IProgress<double> progress
    );
}
