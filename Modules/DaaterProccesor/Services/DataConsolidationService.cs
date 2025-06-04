using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using GestLog.Services;

namespace GestLog.Modules.DaaterProccesor.Services;

public class DataConsolidationService : IDataConsolidationService
{
    private readonly IGestLogLogger _logger;    public DataConsolidationService(IGestLogLogger logger)
    {
        _logger = logger;
    }public DataTable ConsolidarDatos(
        string folderPath,
        Dictionary<string, string> paises,
        Dictionary<long, string[]> partidas,
        Dictionary<string, string> proveedores,
        System.IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        return _logger.LoggedOperation("Consolidación de datos Excel", () =>
        {
            _logger.LogDebug("📊 Iniciando consolidación de datos desde: {FolderPath}", folderPath);
            
            var excelFiles = Directory.GetFiles(folderPath, "*.xlsx");
            _logger.LogDebug("📄 Archivos Excel encontrados: {FileCount}", excelFiles.Length);
            
            var requiredColumns = new List<string>
            {
                "FECHA DECLARACIÓN", "NÚMERO DECLARACIÓN", "IMPORTADOR", "IMPORTADOR",
                "EXPORTADOR (PROVEEDOR)", "DIRECCIÓN EXPORTADOR (PROVEEDOR)", "DATO DE CONTACTO EXPORTADOR",
                "PAÍS EXPORTADOR", "PAÍS DE ORIGEN", "PARTIDA ARANCELARIA", "PARTIDA ARANCELARIA",
                "NÚMERO DE BULTOS", "PESO NETO", "VALOR FOB (USD)", "DESCRIPCIÓN MERCANCÍA"
            };
            
            var culture = new CultureInfo("es-ES");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        var consolidatedData = new DataTable();
        consolidatedData.Columns.Add("Fecha", typeof(DateTime));
        consolidatedData.Columns.Add("Mes", typeof(string));
        consolidatedData.Columns.Add("Número Declaración", typeof(long));
        consolidatedData.Columns.Add("NIT IMPORTADOR", typeof(long));
        consolidatedData.Columns.Add("NOMBRE IMPORTADOR", typeof(string));
        consolidatedData.Columns.Add("NOMBRE PROVEEDOR", typeof(string));
        consolidatedData.Columns.Add("DIRECCIÓN PROVEEDOR", typeof(string));
        consolidatedData.Columns.Add("DATO DE CONTACTO PROVEEDOR", typeof(string));
        consolidatedData.Columns.Add("PAIS EXPORTADOR", typeof(string));
        consolidatedData.Columns.Add("PAIS DE ORIGEN", typeof(string));
        consolidatedData.Columns.Add("NOMBRE PAIS DE ORIGEN", typeof(string));
        consolidatedData.Columns.Add("PARTIDA ARANCELARIA", typeof(long));
        consolidatedData.Columns.Add("NÚMERO DE BULTOS", typeof(string));
        consolidatedData.Columns.Add("DESCRIPCION GENERAL PARTIDA ARANCELARIA", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO PARTIDA ARANCELARIA", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO SUB-PARTIDA", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO SUB-PARTIDA NIVEL 1", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO SUB-SUB-PARTIDA NIVEL 2", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO SUB-SUB-PARTIDA NIVEL 3", typeof(string));
        consolidatedData.Columns.Add("PESO NETO", typeof(double));
        consolidatedData.Columns.Add("PESO TON", typeof(double));
        consolidatedData.Columns.Add("VALOR FOB(USD)", typeof(double));
        consolidatedData.Columns.Add("FOB POR TON", typeof(double));
        consolidatedData.Columns.Add("DESCRIPCION MERCANCIA", typeof(string));        int fileCount = excelFiles.Length;
        int fileIndex = 0;
        var totalRowsProcessed = 0;

        foreach (var file in excelFiles)
        {
            // Verificar cancelación antes de procesar cada archivo
            cancellationToken.ThrowIfCancellationRequested();
            
            var fileName = Path.GetFileName(file);
            _logger.LogDebug("📂 Procesando archivo {FileIndex}/{FileCount}: {FileName}", 
                fileIndex + 1, fileCount, fileName);
            
            try
            {
                using var workbook = new XLWorkbook(file);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogWarning("⚠️ Archivo sin worksheets válidos: {FileName}", fileName);
                    continue;
                }
                
                var headerRow = worksheet.Row(1);
                var missingColumns = requiredColumns
                    .Where(col => !headerRow.Cells().Any(cell => cell.GetString().Trim().Equals(col, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                    
                if (missingColumns.Any())
                {
                    _logger.LogWarning("⚠️ Archivo con columnas faltantes: {FileName} - Columnas: {MissingColumns}", 
                        fileName, string.Join(", ", missingColumns));
                    continue;
                }                var rows = worksheet.RowsUsed().Skip(1).ToList();
                int rowCount = rows.Count;
                int rowIndex = 0;
                
                _logger.LogDebug("📊 Procesando {RowCount} filas del archivo: {FileName}", rowCount, fileName);
                
                foreach (var row in rows)
                {                    // Verificar cancelación cada 100 filas para no impactar mucho el rendimiento
                    if (rowIndex % 100 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (rowIndex > 0)
                        {
                            _logger.LogDebug("⚙️ Progreso archivo {FileName}: {ProcessedRows}/{TotalRows} filas", 
                                fileName, rowIndex, rowCount);
                        }
                    }
                    
                    var fecha = row.Cell(1).GetString();
                    var declaracion = row.Cell(2).GetString();
                    var nitImportador = row.Cell(3).GetString();
                    var nombreImportador = row.Cell(4).GetString();
                    var nombreProveedor = row.Cell(5).GetString();
                    var direccionProveedor = row.Cell(6).GetString();
                    var datoContactoProveedor = row.Cell(7).GetString();
                    var paisExportador = row.Cell(8).GetString();
                    var paisDeOrigen = row.Cell(9).GetString();
                    var partidaArancelaria = row.Cell(10).GetString();
                    var numeroDeBultos = row.Cell(12).GetString();
                    var pesoNeto = row.Cell(13).GetString();
                    var valorFobUsd = row.Cell(14).GetString();
                    var descripcionMercancia = row.Cell(15).GetString();
                    // --- Normalización de nombre de proveedor ---
                    // Obtener lista de nombres oficiales únicos
                    var nombresOficiales = proveedores.Values.Distinct().ToList();
                    if (!string.IsNullOrWhiteSpace(nombreProveedor))
                    {
                        nombreProveedor = ResourceLoaderService.NormalizarNombreProveedor(nombreProveedor, nombresOficiales, 80);
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(direccionProveedor) && proveedores.TryGetValue(direccionProveedor, out var nombreEncontrado))
                            nombreProveedor = nombreEncontrado;
                        else if (!string.IsNullOrWhiteSpace(datoContactoProveedor) && proveedores.TryGetValue(datoContactoProveedor, out nombreEncontrado))
                            nombreProveedor = nombreEncontrado;
                        // Si se encontró, también normalizar
                        if (!string.IsNullOrWhiteSpace(nombreProveedor))
                            nombreProveedor = ResourceLoaderService.NormalizarNombreProveedor(nombreProveedor, nombresOficiales, 80);
                        row.Cell(5).Value = nombreProveedor;
                    }
                    DateTime? fechaValida = null;
                    string mes = string.Empty;
                    if (!string.IsNullOrEmpty(fecha))
                    {
                        if (DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ||
                            DateTime.TryParse(fecha, out parsedDate))
                        {
                            fechaValida = parsedDate;
                            mes = parsedDate.ToString("MMMM", culture).ToUpper();
                        }
                        else
                        {
                            mes = "MES INVÁLIDO";
                        }
                    }
                    else
                    {
                        mes = "MES INVÁLIDO";
                    }
                    if (!string.IsNullOrWhiteSpace(declaracion))
                    {
                        var comaIndex = declaracion.IndexOf(',');
                        if (comaIndex != -1)
                            declaracion = declaracion.Substring(0, comaIndex);
                    }
                    long numeroDeclaracion = 0;
                    if (!string.IsNullOrWhiteSpace(declaracion))
                    {
                        declaracion = declaracion.Replace(",", "");
                        if (long.TryParse(declaracion, out var parsedDeclaracion))
                            numeroDeclaracion = parsedDeclaracion;
                    }
                    if (!string.IsNullOrWhiteSpace(nitImportador))
                    {
                        var comaIndex = nitImportador.IndexOf(',');
                        if (comaIndex != -1)
                            nitImportador = nitImportador.Substring(0, comaIndex);
                    }
                    long numeroNitImportador = 0;
                    if (!string.IsNullOrWhiteSpace(nitImportador))
                    {
                        nitImportador = nitImportador.Replace(",", "");
                        if (long.TryParse(nitImportador, out var parsedNitImportador))
                            numeroNitImportador = parsedNitImportador;
                    }
                    if (!string.IsNullOrWhiteSpace(paisExportador) && (int.TryParse(paisExportador, out _) || paisExportador.Any(char.IsDigit)))
                        paisExportador = "CO";
                    if (!string.IsNullOrWhiteSpace(paisDeOrigen) && (int.TryParse(paisDeOrigen, out _) || paisDeOrigen.Any(char.IsDigit)))
                        paisDeOrigen = "CO";
                    string nombrePaisDeOrigen = paises.ContainsKey(paisDeOrigen) ? paises[paisDeOrigen] : "Desconocido";
                    long numeroPartidaArancelaria = 0;
                    if (!string.IsNullOrWhiteSpace(partidaArancelaria))
                    {
                        partidaArancelaria = partidaArancelaria.Replace(",", "");
                        if (long.TryParse(partidaArancelaria, out var parsedPartida))
                            numeroPartidaArancelaria = parsedPartida;
                    }
                    var descripcionGeneral = "Desconocido";
                    var significadoPartida = "Desconocido";
                    var significadoSubPartida = "Desconocido";
                    var significadoSubPartidaNivel1 = "Desconocido";
                    var significadoSubSubPartidaNivel2 = "Desconocido";
                    var significadoSubSubPartidaNivel3 = "Desconocido";
                    if (partidas.TryGetValue(numeroPartidaArancelaria, out var partidaData))
                    {
                        descripcionGeneral = partidaData[0];
                        significadoPartida = partidaData[1];
                        significadoSubPartida = partidaData[2];
                        significadoSubPartidaNivel1 = partidaData[3];
                        significadoSubSubPartidaNivel2 = partidaData[4];
                        significadoSubSubPartidaNivel3 = partidaData[5];
                    }
                    double pesoNetoValue = 0;
                    if (!string.IsNullOrWhiteSpace(pesoNeto))
                    {
                        pesoNeto = pesoNeto.Replace(",", "");
                        if (double.TryParse(pesoNeto, out var parsedPesoNeto))
                            pesoNetoValue = parsedPesoNeto;
                    }
                    double pesoTonValue = pesoNetoValue / 1000;
                    double valorFobUsdValue = 0;
                    if (!string.IsNullOrWhiteSpace(valorFobUsd))
                    {
                        valorFobUsd = valorFobUsd.Replace(",", "");
                        if (double.TryParse(valorFobUsd, out var parsedValorFobUsd))
                            valorFobUsdValue = parsedValorFobUsd;
                    }
                    double fobPorTonValue = pesoTonValue > 0 ? valorFobUsdValue / pesoTonValue : 0;
                    consolidatedData.Rows.Add(
                        fechaValida, mes, numeroDeclaracion, numeroNitImportador, nombreImportador, nombreProveedor,
                        direccionProveedor, datoContactoProveedor, paisExportador, paisDeOrigen, nombrePaisDeOrigen,
                        numeroPartidaArancelaria, numeroDeBultos, descripcionGeneral, significadoPartida, significadoSubPartida,
                        significadoSubPartidaNivel1, significadoSubSubPartidaNivel2, significadoSubSubPartidaNivel3,
                        pesoNetoValue, pesoTonValue, valorFobUsdValue, fobPorTonValue, descripcionMercancia);
                    rowIndex++;                    // Reporta progreso granular por fila
                    double progressValue = ((double)fileIndex + (rowCount > 0 ? (double)rowIndex / rowCount : 0)) * 100.0 / fileCount;
                    progress?.Report(Math.Min(Math.Max(progressValue, 0), 100)); // Asegurar valor entre 0-100
                }
                
                totalRowsProcessed += rowCount;
                _logger.LogDebug("✅ Archivo completado: {FileName} - {RowCount} filas procesadas", fileName, rowCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando archivo: {FileName}", fileName);
            }
            fileIndex++;        }
        
        _logger.LogDebug("🔄 Ordenando datos consolidados...");
        
        // Ordenar los datos por el número del mes y luego por "PARTIDA ARANCELARIA"
        var sortedData = consolidatedData.AsEnumerable()
            .OrderBy(row =>
            {
                var mes = row.Field<string>("Mes");
                return !string.IsNullOrEmpty(mes)
                    ? DateTime.ParseExact(mes, "MMMM", new CultureInfo("es-ES")).Month
                    : 0;
            })
            .ThenBy(row => row.Field<long>("PARTIDA ARANCELARIA"))
            .CopyToDataTable();
            
        _logger.LogDebug("✅ Consolidación completada: {TotalFiles} archivos, {TotalRows} filas, {ConsolidatedRows} filas consolidadas", 
            fileCount, totalRowsProcessed, sortedData.Rows.Count);
            
        return sortedData;
        });
    }
}
