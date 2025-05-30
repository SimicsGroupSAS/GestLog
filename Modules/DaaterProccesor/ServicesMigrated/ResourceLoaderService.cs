using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using ClosedXML.Excel;
using FuzzySharp;

namespace GestLog.ServicesMigrated;

public class ResourceLoaderService : IResourceLoaderService
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    /// <summary>
    /// Obtiene un stream del archivo, primero intentando como embedded resource, luego como archivo físico
    /// </summary>
    private Stream GetFileStream(string fileName)
    {
        // Intentar primero como embedded resource
        var resourceName = $"GestLog.Data.{fileName}";
        var stream = _assembly.GetManifestResourceStream(resourceName);
        
        if (stream != null)
        {
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

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read);
            }
        }

        throw new FileNotFoundException($"No se encontró el archivo {fileName} ni como embedded resource ni como archivo físico en ninguna de las rutas esperadas: {string.Join(", ", possiblePaths)}");
    }    public Dictionary<string, string> LoadPaises()
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var stream = GetFileStream("paises_iso.xlsx");
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet != null)
        {
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var nombrePais = row.Cell(1).GetString();
                var codigoIso = row.Cell(4).GetString();
                if (!string.IsNullOrWhiteSpace(nombrePais) && !string.IsNullOrWhiteSpace(codigoIso))
                    mapping[codigoIso] = nombrePais;
            }
        }
        return mapping;
    }    public Dictionary<long, string[]> LoadPartidas()
    {
        var mapping = new Dictionary<long, string[]>();
        using var stream = GetFileStream("PartidasArancelarias.xlsx");
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet != null)
        {
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
                }
            }
        }
        return mapping;
    }    public Dictionary<string, string> LoadProveedores()
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var stream = GetFileStream("ListadoExportExtranjAcero.xlsx");
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet != null)
        {
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var nombreProveedor = row.Cell(1).GetString();
                var direccionProveedor = row.Cell(2).GetString();
                var contactoProveedor = row.Cell(3).GetString();
                if (!string.IsNullOrWhiteSpace(direccionProveedor))
                    mapping[direccionProveedor] = nombreProveedor;
                if (!string.IsNullOrWhiteSpace(contactoProveedor))
                    mapping[contactoProveedor] = nombreProveedor;
            }
        }
        return mapping;
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
