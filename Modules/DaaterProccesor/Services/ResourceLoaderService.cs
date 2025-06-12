using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using ClosedXML.Excel;
using FuzzySharp;
using GestLog.Services.Core.Logging;
using GestLog.Modules.DaaterProccesor.Exceptions;

namespace GestLog.Modules.DaaterProccesor.Services;

public class ResourceLoaderService : IResourceLoaderService
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly IGestLogLogger _logger;    public ResourceLoaderService(IGestLogLogger logger)
    {
        _logger = logger;
    }/// <summary>
    /// Obtiene un stream del archivo, primero intentando como embedded resource, luego como archivo físico
    /// </summary>
    private Stream GetFileStream(string fileName)
    {
        try
        {
            _logger.LogDebug("🔍 Buscando archivo de recursos: {FileName}", fileName);
            
            // Intentar primero como embedded resource
            var resourceName = $"GestLog.Data.{fileName}";
            var stream = _assembly.GetManifestResourceStream(resourceName);
            
            if (stream != null)
            {
                _logger.LogDebug("✅ Archivo encontrado como embedded resource: {ResourceName}", resourceName);
                return stream;
            }

            // Si no se encuentra como embedded resource, buscar como archivo físico
            var possiblePaths = new[]
            {
                Path.Combine("Data", fileName),
                Path.Combine("..", "..", "..", "Data", fileName), // Para desarrollo
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", fileName),
                fileName
            };

            _logger.LogDebug("🔍 Buscando como archivo físico en rutas: {Paths}", string.Join(", ", possiblePaths));

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogDebug("✅ Archivo encontrado en ruta física: {FilePath}", path);
                    return new FileStream(path, FileMode.Open, FileAccess.Read);
                }
            }            var errorMessage = $"No se encontró el archivo {fileName} ni como embedded resource ni como archivo físico en ninguna de las rutas esperadas: {string.Join(", ", possiblePaths)}";
            _logger.LogError(new FileNotFoundException(errorMessage), "❌ Error al buscar archivo de recursos: {FileName}", fileName);
            throw new ResourceException($"Recurso no encontrado: {fileName}", fileName);
        }
        catch (Exception ex) when (!(ex is FileNotFoundException))
        {
            _logger.LogError(ex, "❌ Error inesperado al acceder al archivo: {FileName}", fileName);
            throw;
        }
    }    public Dictionary<string, string> LoadPaises()
    {
        return _logger.LoggedOperation("Carga de países ISO", () =>
        {
            _logger.LogDebug("🌍 Iniciando carga de mapping de países ISO");
            
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var stream = GetFileStream("paises_iso.xlsx");
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet != null)
            {
                var rowCount = 0;
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var nombrePais = row.Cell(1).GetString();
                    var codigoIso = row.Cell(4).GetString();
                    if (!string.IsNullOrWhiteSpace(nombrePais) && !string.IsNullOrWhiteSpace(codigoIso))
                    {
                        mapping[codigoIso] = nombrePais;
                        rowCount++;
                    }
                }
                _logger.LogDebug("✅ Países cargados exitosamente: {Count} países procesados", rowCount);
            }
            else
            {
                _logger.LogWarning("⚠️ No se encontró worksheet en archivo paises_iso.xlsx");
            }
            
            return mapping;
        });
    }    public Dictionary<long, string[]> LoadPartidas()
    {
        return _logger.LoggedOperation("Carga de partidas arancelarias", () =>
        {
            _logger.LogDebug("📦 Iniciando carga de partidas arancelarias");
            
            var mapping = new Dictionary<long, string[]>();
            using var stream = GetFileStream("PartidasArancelarias.xlsx");
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet != null)
            {
                var rowCount = 0;
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var partida = row.Cell(1).GetString();
                    if (long.TryParse(partida, out var partidaKey))
                    {
                        var descripcionGeneral = row.Cell(2).GetString();
                        var significadoPartida = row.Cell(3).GetString();
                        var significadoSubPartida = row.Cell(4).GetString();
                        var significadoSubPartidaNivel1 = row.Cell(5).GetString();
                        var significadoSubSubPartidaNivel2 = row.Cell(6).GetString();
                        var significadoSubSubPartidaNivel3 = row.Cell(7).GetString();
                        mapping[partidaKey] = new[]
                        {
                            descripcionGeneral,
                            significadoPartida,
                            significadoSubPartida,
                            significadoSubPartidaNivel1,
                            significadoSubSubPartidaNivel2,
                            significadoSubSubPartidaNivel3
                        };
                        rowCount++;
                    }
                }
                _logger.LogDebug("✅ Partidas arancelarias cargadas exitosamente: {Count} partidas procesadas", rowCount);
            }
            else
            {
                _logger.LogWarning("⚠️ No se encontró worksheet en archivo PartidasArancelarias.xlsx");
            }
            
            return mapping;
        });
    }    public Dictionary<string, string> LoadProveedores()
    {
        return _logger.LoggedOperation("Carga de proveedores", () =>
        {
            _logger.LogDebug("🏭 Iniciando carga de mapping de proveedores");
            
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var stream = GetFileStream("ListadoExportExtranjAcero.xlsx");
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet != null)
            {
                var rowCount = 0;
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var nombreProveedor = row.Cell(1).GetString();
                    var direccionProveedor = row.Cell(2).GetString();
                    var contactoProveedor = row.Cell(3).GetString();
                    
                    if (!string.IsNullOrWhiteSpace(direccionProveedor))
                    {
                        mapping[direccionProveedor] = nombreProveedor;
                        rowCount++;
                    }
                    if (!string.IsNullOrWhiteSpace(contactoProveedor))
                    {
                        mapping[contactoProveedor] = nombreProveedor;
                        rowCount++;
                    }
                }
                _logger.LogDebug("✅ Proveedores cargados exitosamente: {Count} mappings procesados", rowCount);
            }
            else
            {
                _logger.LogWarning("⚠️ No se encontró worksheet en archivo ListadoExportExtranjAcero.xlsx");
            }
            
            return mapping;
        });
    }
    /// <summary>
    /// Normaliza un nombre de proveedor usando la lista oficial de nombres del archivo ListadoExportExtranjAcero.xlsx
    /// </summary>
    /// <param name="nombreEntrada">Nombre de proveedor a normalizar</param>
    /// <param name="proveedoresOficiales">Lista de nombres oficiales</param>
    /// <param name="umbral">Porcentaje mínimo de similitud (0-100)</param>
    /// <returns>Nombre oficial más parecido o el original si no supera el umbral</returns>
    public static string NormalizarNombreProveedor(string nombreEntrada, IEnumerable<string> proveedoresOficiales, int umbral = 85)
    {
        if (string.IsNullOrWhiteSpace(nombreEntrada) || proveedoresOficiales == null)
            return nombreEntrada;
        var mejor = Process.ExtractOne(nombreEntrada, proveedoresOficiales);
        if (mejor != null && mejor.Score >= umbral)
            return mejor.Value;
        return nombreEntrada;
    }
}
