using System.Data;

namespace GestLog.ServicesMigrated;

public interface IConsolidatedFilterService
{
    DataTable FilterRows(DataTable consolidatedTable);
}
