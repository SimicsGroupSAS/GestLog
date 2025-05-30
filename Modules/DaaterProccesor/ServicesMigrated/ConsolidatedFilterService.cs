using System.Data;
using System.Linq;

namespace GestLog.ServicesMigrated;

public class ConsolidatedFilterService : IConsolidatedFilterService
{
    public DataTable FilterRows(DataTable consolidatedTable)
    {
        // Primer filtro: columna DESCRIPCION GENERAL PARTIDA ARANCELARIA
        string colFiltro1 = "DESCRIPCION GENERAL PARTIDA ARANCELARIA";
        if (!consolidatedTable.Columns.Contains(colFiltro1))
        {
            System.Windows.MessageBox.Show($"No se encontró la columna '{colFiltro1}' en el DataTable.", "Error filtro");
            return consolidatedTable;
        }
        var primerFiltrado = consolidatedTable.AsEnumerable()
            .Where(row => row[colFiltro1]?.ToString() == "Productos planos laminados en caliente"
                       || row[colFiltro1]?.ToString() == "Perfiles");

        // Segundo filtro: columna SIGNIFICADO SUB-PARTIDA NIVEL 1
        string colFiltro2 = "SIGNIFICADO SUB-PARTIDA NIVEL 1";
        if (!consolidatedTable.Columns.Contains(colFiltro2))
        {
            System.Windows.MessageBox.Show($"No se encontró la columna '{colFiltro2}' en el DataTable.", "Error filtro");
            return consolidatedTable;
        }
        string[] valoresPermitidos = new[] {
            "Ángulos",
            "De espesor superior a 10 mm",
            "De espesor superior a 4,75 mm",
            "De espesor superior o igual a 3 mm pero inferior a 4,75 mm",
            "De espesor superior o igual a 3 mm pero inferior o igual a 4,75 mm",
            "De espesor superior o igual a 4,75 mm pero inferior o igual a 10 mm",
            "Perfiles en H (Vigas en H)",
            "Perfiles en I (Vigas en I)",
            "Perfiles en L (Ángulos)",
            "Perfiles en U (Canales)"
        };
        var segundoFiltrado = primerFiltrado
            .Where(row => valoresPermitidos.Contains(row[colFiltro2]?.ToString() ?? string.Empty));

        if (!segundoFiltrado.Any())
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas

        return segundoFiltrado.CopyToDataTable();
    }
}
