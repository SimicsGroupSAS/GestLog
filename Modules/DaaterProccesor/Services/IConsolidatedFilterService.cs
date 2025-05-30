using System.Data;

namespace GestLog.Modules.DaaterProccesor.Services;

public interface IConsolidatedFilterService
{
    DataTable FilterRows(DataTable consolidatedTable);
}
