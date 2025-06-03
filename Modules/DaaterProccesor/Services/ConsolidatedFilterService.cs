using System.Data;
using System.Linq;
using GestLog.Services;

namespace GestLog.Modules.DaaterProccesor.Services;

public class ConsolidatedFilterService : IConsolidatedFilterService
{
    private readonly IGestLogLogger _logger;

    public ConsolidatedFilterService(IGestLogLogger logger)
    {
        _logger = logger;
    }

    public DataTable FilterRows(DataTable consolidatedTable)
    {
        _logger.LogDebug("🔍 Iniciando filtrado de datos consolidados: {RowCount} filas", consolidatedTable.Rows.Count);
        
        // Primer filtro: columna DESCRIPCION GENERAL PARTIDA ARANCELARIA
        string colFiltro1 = "DESCRIPCION GENERAL PARTIDA ARANCELARIA";
        if (!consolidatedTable.Columns.Contains(colFiltro1))
        {
            _logger.LogWarning("⚠️ No se encontró la columna requerida para filtro: {ColumnName}", colFiltro1);
            System.Windows.MessageBox.Show($"No se encontró la columna '{colFiltro1}' en el DataTable.", "Error filtro");
            return consolidatedTable;
        }
        
        _logger.LogDebug("🔎 Aplicando primer filtro por: {ColumnName}", colFiltro1);
        var primerFiltrado = consolidatedTable.AsEnumerable()
            .Where(row => row[colFiltro1]?.ToString() == "Productos planos laminados en caliente"
                       || row[colFiltro1]?.ToString() == "Perfiles");

        // Segundo filtro: columna SIGNIFICADO SUB-PARTIDA NIVEL 1
        string colFiltro2 = "SIGNIFICADO SUB-PARTIDA NIVEL 1";
        if (!consolidatedTable.Columns.Contains(colFiltro2))
        {
            _logger.LogWarning("⚠️ No se encontró la columna requerida para filtro: {ColumnName}", colFiltro2);
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
        
        _logger.LogDebug("🔎 Aplicando segundo filtro por: {ColumnName} con {ValuesCount} valores permitidos", 
            colFiltro2, valoresPermitidos.Length);
            
        var segundoFiltrado = primerFiltrado
            .Where(row => valoresPermitidos.Contains(row[colFiltro2]?.ToString() ?? string.Empty));

        if (!segundoFiltrado.Any())
        {
            _logger.LogWarning("⚠️ Filtrado no produjo resultados");
            return consolidatedTable.Clone(); // Retorna tabla vacía con mismas columnas
        }

        var resultado = segundoFiltrado.CopyToDataTable();
        _logger.LogDebug("✅ Filtrado completado: {FilteredRowCount} filas resultantes de {OriginalRowCount}", 
            resultado.Rows.Count, consolidatedTable.Rows.Count);
            
        return resultado;
    }
}
