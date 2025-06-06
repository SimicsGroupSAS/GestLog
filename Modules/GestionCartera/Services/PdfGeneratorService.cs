// filepath: e:\Softwares\GestLog\Modules\GestionCartera\Services\PdfGeneratorService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.Globalization;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Pdf.Canvas;
using iText.IO.Image;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using GestLog.Services.Core.Logging;

// Resolver ambigüedades de tipos
using Rectangle = iText.Kernel.Geom.Rectangle;
using Path = System.IO.Path;

namespace GestLog.Modules.GestionCartera.Services;

/// <summary>
/// Manejador para agregar imagen de fondo a los PDFs
/// </summary>
public class BackgroundImageHandler : IEventHandler
{
    private readonly ImageData backgroundImage;
    private readonly IGestLogLogger _logger;

    public BackgroundImageHandler(string imagePath, IGestLogLogger logger)
    {
        _logger = logger;
        
        if (File.Exists(imagePath))
        {
            try
            {
                backgroundImage = ImageDataFactory.Create(imagePath);
                _logger.LogInformation("Plantilla de fondo cargada correctamente: {ImagePath}", imagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la imagen de fondo: {ImagePath}", imagePath);
                throw;
            }
        }
        else
        {
            throw new FileNotFoundException($"No se encontró el archivo de plantilla: {imagePath}");
        }
    }

    public void HandleEvent(Event @event)
    {
        if (@event is PdfDocumentEvent docEvent)
        {
            PdfDocument pdf = docEvent.GetDocument();
            PdfPage page = docEvent.GetPage();
            Rectangle pageSize = page.GetPageSize();
            
            PdfCanvas canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdf);
            
            try
            {
                canvas.AddImageFittedIntoRectangle(backgroundImage, pageSize, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al dibujar fondo en PDF");
            }
            finally
            {
                canvas.Release();
            }
        }
    }
}

/// <summary>
/// Implementación del servicio de generación de PDFs adaptado para la estructura de Excel de MiProyectoWPF
/// </summary>
public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly IGestLogLogger _logger;
    private readonly List<GeneratedPdfInfo> _generatedPdfs = new();
    private const int EXCEL_HEADER_ROW = 4; // La fila 4 es el encabezado en el formato original

    public PdfGeneratorService(IGestLogLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ValidateExcelStructureAsync(string excelFilePath)
    {
        try
        {
            if (!File.Exists(excelFilePath))
            {
                _logger.LogWarning("Archivo Excel no encontrado en: {FilePath}", excelFilePath);
                return false;
            }

            bool result = false;
            
            await Task.Run(() => {
                try
                {
                    using var workbook = new XLWorkbook(excelFilePath);
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    
                    if (worksheet == null)
                    {
                        _logger.LogWarning("El archivo Excel no contiene hojas de trabajo");
                        return;
                    }
                    
                    // Verificar que hay contenido en el Excel
                    if (!worksheet.CellsUsed().Any())
                    {
                        _logger.LogWarning("El archivo Excel está vacío");
                        return;
                    }

                    // En el proyecto original, las cabeceras están en la fila 4
                    var headerRow = worksheet.Row(EXCEL_HEADER_ROW);
                    
                    if (!headerRow.CellsUsed().Any())
                    {
                        _logger.LogWarning("La fila de encabezados está vacía");
                        return;
                    }

                    // Buscar columnas clave según el formato original (como se ve en SimplePdfGenerator.cs)
                    var columns = headerRow.CellsUsed().Select(c => c.Value.ToString()).ToList();
                    _logger.LogInformation("Columnas encontradas en fila {Row}: {Columns}", 
                        EXCEL_HEADER_ROW, string.Join(", ", columns));

                    // Verificar si tiene las columnas esenciales del formato original
                    bool hasNombres = columns.Any(c => c.Contains("Nombres", StringComparison.OrdinalIgnoreCase));
                    bool hasIdentificacion = columns.Any(c => c.Contains("Identificacion", StringComparison.OrdinalIgnoreCase));
                    bool hasValorTotal = columns.Any(c => 
                        c.Contains("Valor Total", StringComparison.OrdinalIgnoreCase) || 
                        c.Contains("ValorTotal", StringComparison.OrdinalIgnoreCase));

                    _logger.LogInformation("Validación de columnas - Nombres: {HasNombres}, Identificacion: {HasId}, Valor: {HasValor}",
                        hasNombres, hasIdentificacion, hasValorTotal);
                        
                    // Si no encuentra las columnas principales
                    if (!hasNombres || !hasIdentificacion || !hasValorTotal)
                    {
                        _logger.LogWarning("El Excel no tiene la estructura esperada. Faltan columnas obligatorias.");
                        _logger.LogWarning("Debe incluir columnas: Nombres, Identificacion y Valor Total");
                        return;
                    }
                    
                    // Verificar que hay datos después de la fila de encabezado
                    int dataRowCount = worksheet.RowsUsed().Count(r => r.RowNumber() > EXCEL_HEADER_ROW);
                    
                    if (dataRowCount == 0)
                    {
                        _logger.LogWarning("El archivo Excel no contiene datos después del encabezado");
                        return;
                    }
                    
                    _logger.LogInformation("Validación exitosa. El archivo contiene {Count} registros de datos", dataRowCount);
                    result = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando Excel: {Path}", excelFilePath);
                }
            });
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar estructura del Excel: {FilePath}", excelFilePath);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetCompaniesPreviewAsync(string excelFilePath)
    {
        try
        {
            return await Task.Run(() =>
            {
                var companies = new HashSet<string>();
                
                using var workbook = new XLWorkbook(excelFilePath);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                    return Enumerable.Empty<string>();

                // Usar la fila 4 como encabezado según el formato original
                var headerRow = worksheet.Row(EXCEL_HEADER_ROW);
                var nombresColumnIndex = -1;
                
                // Buscar la columna "Nombres"
                foreach (var cell in headerRow.CellsUsed())
                {
                    var cellValue = cell.Value.ToString();
                    if (cellValue.Contains("Nombres", StringComparison.OrdinalIgnoreCase) && 
                        !cellValue.Contains("Etiqueta", StringComparison.OrdinalIgnoreCase))
                    {
                        nombresColumnIndex = cell.Address.ColumnNumber;
                        _logger.LogInformation("Columna \'Nombres\' encontrada en posición {Index}", nombresColumnIndex);
                        break;
                    }
                }

                if (nombresColumnIndex <= 0)
                {
                    _logger.LogWarning("No se pudo encontrar la columna \'Nombres\' en el Excel");
                    return Enumerable.Empty<string>();
                }

                // Leer los nombres de empresa desde la fila 5 en adelante
                foreach (var row in worksheet.RowsUsed())
                {
                    // Saltarse las filas de encabezado
                    if (row.RowNumber() <= EXCEL_HEADER_ROW)
                        continue;
                        
                    // Leer el valor de la columna Nombres
                    var companyName = row.Cell(nombresColumnIndex).Value.ToString()?.Trim();
                    
                    // Solo agregar si es un nombre válido (no total, no vacío)
                    if (!string.IsNullOrEmpty(companyName) && 
                        !companyName.Contains("Total", StringComparison.OrdinalIgnoreCase) &&
                        !companyName.Equals("Cliente", StringComparison.OrdinalIgnoreCase))
                    {
                        companies.Add(companyName);
                    }
                }

                _logger.LogInformation("Se encontraron {Count} empresas en el Excel", companies.Count);
                return companies.OrderBy(c => c);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener vista previa de empresas: {FilePath}", excelFilePath);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IReadOnlyList<GeneratedPdfInfo>> GenerateEstadosCuentaAsync(
        string excelFilePath, 
        string outputFolder,
        string? templatePath = null,
        IProgress<(int current, int total, string status)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _generatedPdfs.Clear();
        
        try
        {
            _logger.LogInformation("🎯 Iniciando generación de estados de cuenta desde: {ExcelFilePath}", excelFilePath);
            
            // Validar archivo de entrada
            if (!File.Exists(excelFilePath))
            {
                throw new FileNotFoundException($"No se encontró el archivo Excel: {excelFilePath}");
            }

            // Crear directorio de salida si no existe
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                _logger.LogInformation("📁 Directorio de salida creado: {OutputFolder}", outputFolder);
            }

            // Limpiar carpeta de salida antes de generar nuevos documentos
            _logger.LogInformation("🧹 Limpiando carpeta de salida...");
            CleanOutputFolder(outputFolder);

            progress?.Report((0, 0, "Leyendo archivo Excel..."));

            // Usar la implementación actualizada para leer los datos del Excel con formato MiProyectoWPF
            var clientGroups = await ReadExcelDataAsync(excelFilePath, cancellationToken);
            
            if (!clientGroups.Any())
            {
                _logger.LogWarning("No se encontraron datos válidos en el archivo Excel");
                return _generatedPdfs.AsReadOnly();
            }

            _logger.LogInformation("📊 Se encontraron {Count} empresas para procesar", clientGroups.Count);

            // Lista para rastrear empresas sin facturas recientes
            var empresasSinFacturasRecientes = new List<string>();
            int empresasProcesadas = 0;
            int total = clientGroups.Count;

            // Generar documentos para cada cliente
            foreach (var clientGroup in clientGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                empresasProcesadas++;
                string statusPrefix = $"[{empresasProcesadas}/{total}]";
                progress?.Report((empresasProcesadas, total, $"Generando PDF para {clientGroup.Value.Nombre}..."));

                try
                {
                    // Eliminar filtro de días >= -8 (usar todas las filas como en el original)
                    var filteredRows = clientGroup.Value.Rows.ToList();

                    if (filteredRows.Count == 0)
                    {
                        empresasSinFacturasRecientes.Add(clientGroup.Value.Nombre);
                        _logger.LogWarning("{StatusPrefix} Cliente {ClientName} no tiene facturas. Omitiendo.", 
                            statusPrefix, clientGroup.Value.Nombre);
                        continue;
                    }

                    _logger.LogInformation("{StatusPrefix} Generando documento para {ClientName}", 
                        statusPrefix, clientGroup.Value.Nombre);
                    
                    var pdfInfo = await GenerateDocumentForClientAsync(
                        clientGroup.Value, 
                        outputFolder, 
                        templatePath,
                        cancellationToken);
                    
                    if (pdfInfo != null)
                    {
                        _generatedPdfs.Add(pdfInfo);
                        _logger.LogInformation("✅ PDF generado exitosamente para {Company}: {RutaArchivo}", 
                            clientGroup.Value.Nombre, pdfInfo.RutaArchivo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al generar PDF para empresa {Company}", clientGroup.Value.Nombre);
                }
            }

            progress?.Report((total, total, "Generación completada"));
            
            _logger.LogInformation("📄 Resumen de procesamiento:");
            _logger.LogInformation("📄 Total de clientes únicos: {ClientCount}", clientGroups.Count);
            _logger.LogInformation("📄 Total de PDFs generados: {PdfCount}", _generatedPdfs.Count);

            // Mostrar empresas sin facturas
            if (empresasSinFacturasRecientes.Count > 0)
            {
                _logger.LogInformation("⚠️ Clientes sin facturas:");
                foreach (var empresa in empresasSinFacturasRecientes)
                {
                    _logger.LogInformation("  - {EmpresaName}", empresa);
                }
            }

            // Guardar lista de PDFs generados para posterior envío
            if (_generatedPdfs.Count > 0)
            {
                SaveGeneratedPdfsList(outputFolder);
            }

            return _generatedPdfs.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la generación de estados de cuenta");
            throw;
        }
    }

    // Actualizado para usar el formato exacto del SimplePdfGenerator original
    private async Task<Dictionary<string, ClienteInfo>> ReadExcelDataAsync(
        string excelFilePath, 
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var clientGroups = new Dictionary<string, ClienteInfo>();
            
            using var workbook = new XLWorkbook(excelFilePath);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
                return clientGroups;

            // Configurar cultura española
            CultureInfo.CurrentCulture = new CultureInfo("es-CO");
            
            // Determinar hasta qué fila hay datos
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow == 0)
            {
                _logger.LogWarning("No se encontraron filas en el archivo Excel");
                return clientGroups;
            }
            
            _logger.LogInformation("Última fila con datos: {LastRow}", lastRow);
            
            // Inicia en la fila 5 (equivalente a Skip(4)) según SimplePdfGenerator original
            int startRow = 5;
            _logger.LogInformation("Comenzando lectura desde la fila {StartRow}", startRow);
            
            int totalEmpresasEncontradas = 0;
            
            // Leer fila por fila desde startRow hasta la última
            for (int rowNum = startRow; rowNum <= lastRow; rowNum++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try 
                {
                    var row = worksheet.Row(rowNum);
                    
                    // Si la fila está vacía, saltarla
                    if (row.IsEmpty())
                    {
                        continue;
                    }
                    
                    // Leer el nombre del cliente (columna B) y el NIT (columna C)
                    string nombre = TryGetStringValue(row.Cell("B"), $"Error al leer nombre en fila {rowNum}").Trim();
                    string nit = TryGetStringValue(row.Cell("C"), $"Error al leer NIT en fila {rowNum}").Trim();
                    
                    // Si no hay nombre o contiene "Total", saltamos esta fila
                    if (string.IsNullOrEmpty(nombre) || nombre.Contains("Total", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    totalEmpresasEncontradas++;
                    _logger.LogDebug("Procesando fila {RowNum} para cliente: {Nombre}, NIT: {Nit}", rowNum, nombre, nit);
                    
                    // Leer los datos de la fila con las columnas correctas: L, M, N, O, U
                    var clienteRow = new ClienteRow
                    {
                        Numero = TryGetStringValue(row.Cell("L"), $"Error al leer número en fila {rowNum} para cliente {nombre}"),
                        Fecha = TryGetDateTimeValue(row.Cell("M"), DateTime.Now, $"Error al leer fecha en fila {rowNum} para cliente {nombre}"),
                        FechaVence = TryGetDateTimeValue(row.Cell("N"), DateTime.Now, $"Error al leer fecha de vencimiento en fila {rowNum} para cliente {nombre}"),
                        ValorTotal = TryGetDoubleValue(row.Cell("O"), 0, $"Error al leer valor total en fila {rowNum} para cliente {nombre}"),
                        NumDias = TryGetIntValue(row.Cell("U"), 0, $"Error al leer días en fila {rowNum} para cliente {nombre}")
                    };
                    
                    // Crear una clave única combinando nombre y NIT
                    string key = string.IsNullOrEmpty(nit) ? nombre : $"{nombre}_{nit}";
                    
                    // Si el cliente no existe en el diccionario, crearlo
                    if (!clientGroups.ContainsKey(key))
                    {
                        clientGroups[key] = new ClienteInfo
                        {
                            Nombre = nombre,
                            Nit = nit,
                            Rows = new List<ClienteRow>()
                        };
                    }
                    
                    // Agregar la fila al grupo correspondiente del cliente
                    clientGroups[key].Rows.Add(clienteRow);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error al procesar fila {RowNum}: {ErrorMessage}", rowNum, ex.Message);
                    // Continuamos con la siguiente fila
                }
            }
            
            _logger.LogInformation("Se encontraron un total de {TotalEmpresas} registros de empresas en el Excel", totalEmpresasEncontradas);
            _logger.LogInformation("Se agruparon en {ClientCount} clientes únicos para procesar", clientGroups.Count);

            return clientGroups;
        }, cancellationToken);
    }

    // Métodos auxiliares para extraer valores de celdas con manejo de errores (igual que SimplePdfGenerator)
    private string TryGetStringValue(IXLCell? cell, string errorMessage)
    {
        try
        {
            if (cell == null || cell.IsEmpty()) return string.Empty;
            return cell.GetString()?.Trim() ?? string.Empty;
        }
        catch (Exception)
        {
            _logger.LogWarning(errorMessage);
            return string.Empty;
        }
    }

    private DateTime TryGetDateTimeValue(IXLCell cell, DateTime defaultValue, string errorMessage)
    {
        try
        {
            if (cell == null || cell.IsEmpty()) return defaultValue;
            return cell.GetDateTime();
        }
        catch (Exception)
        {
            try
            {
                // Intenta extraer como string y convertir
                string dateStr = cell.GetString().Trim();
                if (DateTime.TryParse(dateStr, out DateTime result))
                    return result;
            }
            catch { }
            
            _logger.LogWarning(errorMessage);
            return defaultValue;
        }
    }

    private double TryGetDoubleValue(IXLCell cell, double defaultValue, string errorMessage)
    {
        try
        {
            if (cell == null || cell.IsEmpty()) return defaultValue;
            return cell.GetDouble();
        }
        catch (Exception)
        {
            try
            {
                // Intenta extraer como string y convertir
                string valueStr = cell.GetString().Trim();
                if (double.TryParse(valueStr, out double result))
                    return result;
            }
            catch { }
            
            _logger.LogWarning(errorMessage);
            return defaultValue;
        }
    }

    private int TryGetIntValue(IXLCell cell, int defaultValue, string errorMessage)
    {
        try
        {
            if (cell == null || cell.IsEmpty()) return defaultValue;
            
            // Intentar diferentes métodos para obtener un entero
            if (cell.DataType == XLDataType.Number)
            {
                return Convert.ToInt32(cell.GetDouble());
            }
            else if (cell.DataType == XLDataType.Text)
            {
                string valueStr = cell.GetString().Trim();
                if (int.TryParse(valueStr, out int result))
                    return result;
            }
            
            // Si llegamos aquí, intenta una conversión general desde el valor
            if (!cell.Value.IsBlank)
            {
                try { return Convert.ToInt32(cell.Value.GetNumber()); } 
                catch { }
            }
            
            _logger.LogWarning(errorMessage);
            return defaultValue;
        }
        catch (Exception)
        {
            _logger.LogWarning(errorMessage);
            return defaultValue;
        }
    }

    // Modificar el método para aceptar ClienteInfo en lugar de nombre y lista (igual que SimplePdfGenerator)
    private async Task<GeneratedPdfInfo?> GenerateDocumentForClientAsync(
        ClienteInfo clienteInfo,
        string outputFolder,
        string? templatePath,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            string clientName = clienteInfo.Nombre;
            List<ClienteRow> clientRows = clienteInfo.Rows;
            string nit = clienteInfo.Nit;
            
            _logger.LogInformation("📄 Generando documento para cliente: {ClientName}, NIT: {Nit}", clientName, nit);
            
            // Ya no se filtra por días, se toman todas las filas
            var filteredRows = clientRows.ToList();
            if (filteredRows.Count == 0)
            {
                _logger.LogWarning("Cliente {ClientName} no tiene facturas. Omitiendo.", clientName);
                return null;
            }
            
            try
            {
                // Determinar tipo de cartera
                bool tienePositivos = filteredRows.Any(r => r.NumDias >= 0);
                bool tienePendiente = filteredRows.Any(r => r.NumDias < 0);
                string tipoCartera;
                
                if (tienePositivos)
                {
                    tipoCartera = "Vencida";
                    _logger.LogInformation("Cliente {ClientName}: Tipo de cartera = VENCIDA", clientName);
                }
                else if (tienePendiente)
                {
                    tipoCartera = "Por Vencer";
                    _logger.LogInformation("Cliente {ClientName}: Tipo de cartera = POR VENCER", clientName);
                }
                else
                {
                    _logger.LogInformation("Cliente {ClientName}: Sin cartera vencida o por vencer. Omitiendo.", clientName);
                    return null;
                }
                
                // Generar nombre de archivo válido, incluyendo el NIT si está disponible
                string nombreBase = clientName;
                if (!string.IsNullOrEmpty(nit))
                {
                    nombreBase = $"{clientName}_{nit}";
                }
                
                string nombreArchivo = SanitizeFileName(nombreBase.Length > 50 ? nombreBase.Substring(0, 50) : nombreBase);
                string pdfFileName = $"{nombreArchivo}.pdf";
                string pdfFilePath = System.IO.Path.Combine(outputFolder, pdfFileName);
                
                // Crear PDF directamente con iText7
                CreatePdfDocument(clientName, nit, filteredRows, tienePositivos, pdfFilePath, templatePath);
                
                var generatedPdf = new GeneratedPdfInfo
                {
                    NombreEmpresa = clientName,
                    Nit = nit,
                    RutaArchivo = pdfFilePath,
                    NombreArchivo = pdfFileName,
                    TipoCartera = tipoCartera
                };
                
                _logger.LogInformation("📄 Documento generado para {ClientName} (NIT: {Nit}): {FileName}", 
                    clientName, nit, pdfFileName);
                    
                return generatedPdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al generar documento para {ClientName}", clientName);
                return null;
            }

        }, cancellationToken);
    }

    // Modificar el método para incluir el NIT en el documento (igual que SimplePdfGenerator)
    private void CreatePdfDocument(string clientName, string nit, List<ClienteRow> rows, bool tienePositivos, string outputPath, string? templatePath)
    {
        // Configurar cultura española
        CultureInfo.CurrentCulture = new CultureInfo("es-CO");
        
        // Fecha formateada
        string fechaFormateada = $"Barranquilla, {DateTime.Now.Day} de {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month).ToLower()} de {DateTime.Now.Year}";
        
        try
        {
            // Crear PDF con iText7
            using (var writer = new PdfWriter(outputPath))
            {
                using (var pdf = new PdfDocument(writer))
                {
                    // Establecer explícitamente el tamaño de página
                    pdf.SetDefaultPageSize(PageSize.LETTER);
                    
                    // Configurar el evento de fondo si existe la plantilla PNG
                    if (!string.IsNullOrEmpty(templatePath) && System.IO.File.Exists(templatePath))
                    {
                        try 
                        {
                            _logger.LogInformation("🖼️ Aplicando plantilla PNG como fondo: {TemplatePath}", templatePath);
                            pdf.AddEventHandler(PdfDocumentEvent.START_PAGE, new BackgroundImageHandler(templatePath, _logger));
                            _logger.LogInformation("✅ Plantilla PNG aplicada correctamente");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error al aplicar plantilla PNG como fondo: {TemplatePath}", templatePath);
                            // Continuar sin fondo si hay error
                        }
                    }
                    else 
                    {
                        _logger.LogInformation("⚠️ No se aplicará fondo porque no se encontró la plantilla PNG.");
                    }
                    
                    using (var document = new Document(pdf))
                    {
                        // Ajustar márgenes superior e inferior de la página (igual que SimplePdfGenerator)
                        document.SetMargins(80, 36, 30, 36); // Top, Right, Bottom, Left (en puntos)

                        // Agregar fecha - alineado a la derecha
                        var parrafoFecha = new Paragraph(fechaFormateada)
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetMarginBottom(8);
                        document.Add(parrafoFecha);
                        
                        // Agregar encabezado para cliente - mantener alineación izquierda para encabezados
                        var paraCliente = new Paragraph("Señor(a):")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetMarginTop(8)
                            .SetMarginBottom(2);
                        paraCliente.SetBold();
                        document.Add(paraCliente);
                        
                        // Agregar nombre del cliente y NIT si está disponible
                        Paragraph paraClienteNombre;
                        if (!string.IsNullOrEmpty(nit))
                        {
                            paraClienteNombre = new Paragraph($"{clientName} (NIT: {nit})")
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginBottom(8);
                        }
                        else
                        {
                            paraClienteNombre = new Paragraph(clientName)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginBottom(8);
                        }
                        document.Add(paraClienteNombre);
                        
                        // Preparar asunto
                        var paraAsunto = new Paragraph()
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetMarginBottom(8);
                        var textoAsunto = new Text("Asunto: ");
                        textoAsunto.SetBold();
                        paraAsunto.Add(textoAsunto);
                        
                        string asunto;
                        if (tienePositivos)
                        {
                            asunto = "Estado de Cartera vencida";
                        }
                        else
                        {
                            asunto = "Aviso de proximidad de vencimiento de factura(s)";
                        }
                        
                        paraAsunto.Add(asunto);
                        document.Add(paraAsunto);
                        
                        // Agregar párrafo principal - JUSTIFICADO
                        var parrafoTexto = "Para SIMICS GROUP S.A.S. es muy importante contar con clientes como usted y mantenerlo informado sobre la situación actual de su cartera. Adjuntamos el estado de cuenta correspondiente; si tiene alguna observación, le agradecemos que nos la comunique por este medio para su pronta revisión.";
                        var paraPrincipal = new Paragraph(parrafoTexto)
                            .SetTextAlignment(TextAlignment.JUSTIFIED)
                            .SetMarginTop(8)
                            .SetMarginBottom(10);
                        document.Add(paraPrincipal);
                        
                        // Crear tabla para datos - ajustar al contenido y añadir márgenes
                        var table = new Table(UnitValue.CreatePercentArray(new float[] { 2, 2, 3, 2, 1 }))
                            .UseAllAvailableWidth()
                            .SetMarginTop(20)
                            .SetMarginBottom(20);
                        
                        // Encabezados de tabla con alineación centrada
                        Cell[] headerCells = new Cell[] {
                            new Cell().Add(new Paragraph("Número de Documento")).SetBold().SetTextAlignment(TextAlignment.CENTER),
                            new Cell().Add(new Paragraph("Fecha de Emisión")).SetBold().SetTextAlignment(TextAlignment.CENTER),
                            new Cell().Add(new Paragraph("Fecha de Vencimiento del Documento")).SetBold().SetTextAlignment(TextAlignment.CENTER),
                            new Cell().Add(new Paragraph("Valor Total")).SetBold().SetTextAlignment(TextAlignment.CENTER),
                            new Cell().Add(new Paragraph("Días")).SetBold().SetTextAlignment(TextAlignment.CENTER)
                        };
                        
                        foreach (var cell in headerCells)
                        {
                            table.AddHeaderCell(cell);
                        }
                        
                        // Agregar datos a la tabla con alineación centrada
                        foreach (var row in rows)
                        {
                            table.AddCell(
                                new Cell().Add(new Paragraph(row.Numero))
                                    .SetTextAlignment(TextAlignment.CENTER));
                            
                            table.AddCell(
                                new Cell().Add(new Paragraph(row.Fecha.ToShortDateString()))
                                    .SetTextAlignment(TextAlignment.CENTER));
                            
                            table.AddCell(
                                new Cell().Add(new Paragraph(row.FechaVence.ToShortDateString()))
                                    .SetTextAlignment(TextAlignment.CENTER));
                            
                            table.AddCell(
                                new Cell().Add(new Paragraph(string.Format(CultureInfo.CurrentCulture, "{0:C}", row.ValorTotal)))
                                    .SetTextAlignment(TextAlignment.RIGHT)); // Alineación derecha para valores monetarios
                            
                            table.AddCell(
                                new Cell().Add(new Paragraph(row.NumDias.ToString()))
                                    .SetTextAlignment(TextAlignment.CENTER));
                        }
                        
                        document.Add(table);
                        
                        // Agregar total - alineado a la derecha
                        double valorTotal = rows.Sum(r => r.ValorTotal);
                        var parrafoTotal = new Paragraph()
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetMarginTop(10)
                            .SetMarginBottom(10);
                        var textoTotal = new Text("Total de Deuda: ");
                        textoTotal.SetBold();
                        parrafoTotal.Add(textoTotal);
                        parrafoTotal.Add(string.Format(CultureInfo.CurrentCulture, "{0:C}", valorTotal));
                        document.Add(parrafoTotal);

                        // Crear un Div para agrupar todo el contenido que debe mantenerse junto
                        Div footerGroup = new Div().SetKeepTogether(true);
                        
                        // Agregar texto estándar - JUSTIFICADO
                        var paraEstandar = new Paragraph("El pago de sus facturas nos ayuda a cumplir nuestros compromisos financieros.")
                            .SetTextAlignment(TextAlignment.JUSTIFIED)
                            .SetMarginBottom(8);
                        footerGroup.Add(paraEstandar);

                        // Agregar línea de despedida y firma - alineados a la izquierda
                        var despedida = new Paragraph("Cordialmente,")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetMarginBottom(8);
                        footerGroup.Add(despedida);

                        // Agregar imagen de firma
                        string firmaPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "firma.png");
                        _logger.LogInformation("🖊️ Buscando archivo de firma en: {FirmaPath}", firmaPath);

                        if (System.IO.File.Exists(firmaPath))
                        {
                            try
                            {
                                _logger.LogInformation("✅ Archivo de firma encontrado. Agregando al documento...");
                                ImageData firmaImage = ImageDataFactory.Create(firmaPath);
                                iText.Layout.Element.Image firma = new iText.Layout.Element.Image(firmaImage)
                                    .SetWidth(100)
                                    .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.LEFT);
                                footerGroup.Add(firma);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "❌ Error al agregar firma");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Archivo de firma no encontrado en: {FirmaPath}", firmaPath);
                        }

                        // Agregar datos de contacto
                        footerGroup.Add(new Paragraph("JUAN MANUEL CUERVO").SetBold().SetTextAlignment(TextAlignment.LEFT));
                        footerGroup.Add(new Paragraph("GERENTE FINANCIERO").SetBold().SetTextAlignment(TextAlignment.LEFT));
                        footerGroup.Add(new Paragraph("SIMICS GROUP S.A.S.").SetBold().SetTextAlignment(TextAlignment.LEFT));
                        
                        // Agregar todo el grupo al documento
                        document.Add(footerGroup);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al crear el documento PDF: {OutputPath}", outputPath);
            throw;
        }
    }

    /// <summary>
    /// Limpia la carpeta de salida eliminando todos los archivos PDF existentes
    /// </summary>
    /// <param name="outputFolder">Carpeta de salida a limpiar</param>
    private void CleanOutputFolder(string outputFolder)
    {
        try
        {
            // Eliminar archivos PDF en la carpeta de salida
            if (Directory.Exists(outputFolder))
            {
                var pdfFiles = Directory.GetFiles(outputFolder, "*.pdf");
                foreach (var file in pdfFiles)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogDebug("🗑️ Archivo eliminado: {FileName}", Path.GetFileName(file));
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogWarning("⚠️ Error al eliminar archivo {FileName}: {ErrorMessage}", 
                            Path.GetFileName(file), fileEx.Message);
                    }
                }
                _logger.LogInformation("✅ Carpeta limpiada: {OutputFolder} ({FileCount} archivos PDF eliminados)", 
                    outputFolder, pdfFiles.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al limpiar carpeta de salida: {OutputFolder}", outputFolder);
        }
    }

    /// <summary>
    /// Guarda la lista de PDFs generados en un archivo de registro para posterior envío
    /// </summary>
    /// <param name="outputFolder">Carpeta donde guardar el archivo de registro</param>
    /// <param name="outputFilePath">Ruta personalizada del archivo (opcional)</param>
    public void SaveGeneratedPdfsList(string outputFolder, string? outputFilePath = null)
    {
        string path = outputFilePath ?? System.IO.Path.Combine(outputFolder, "pdfs_generados.txt");
        
        try
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("Fecha de generación: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine($"Total de PDFs generados: {_generatedPdfs.Count}");
                writer.WriteLine("-------------------------------------------------------------");
                
                foreach (var pdf in _generatedPdfs)
                {
                    writer.WriteLine($"Empresa: {pdf.NombreEmpresa}");
                    writer.WriteLine($"NIT: {pdf.Nit}");
                    writer.WriteLine($"Archivo: {pdf.NombreArchivo}");
                    writer.WriteLine($"Tipo: {pdf.TipoCartera}");
                    writer.WriteLine($"Ruta: {pdf.RutaArchivo}");
                    writer.WriteLine("-------------------------------------------------------------");
                }
            }
            
            _logger.LogInformation("📄 Lista de PDFs generados guardada en: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al guardar lista de PDFs generados: {Path}", path);
        }
    }

    /// <summary>
    /// Identifica PDFs que no tienen correspondencia en la lista de empresas del Excel
    /// </summary>
    /// <param name="empresasEnExcel">Lista de empresas encontradas en el Excel</param>
    /// <returns>Lista de PDFs sin correspondencia en el Excel</returns>
    public List<GeneratedPdfInfo> IdentifyOrphanedPdfs(List<string> empresasEnExcel)
    {
        var orphanedPdfs = new List<GeneratedPdfInfo>();
        
        if (empresasEnExcel == null)
        {
            _logger.LogWarning("❌ Lista de empresas nula al identificar PDFs huérfanos");
            return orphanedPdfs;
        }
        
        foreach (var pdf in _generatedPdfs)
        {
            bool found = false;
            
            // Buscar por nombre de empresa exacto
            if (empresasEnExcel.Contains(pdf.NombreEmpresa, StringComparer.OrdinalIgnoreCase))
            {
                found = true;
            }
            // También buscar por nombre truncado (para nombres largos)
            else if (pdf.NombreEmpresa.Length > 30)
            {
                string nombreTruncado = pdf.NombreEmpresa.Substring(0, 30);
                if (empresasEnExcel.Contains(nombreTruncado, StringComparer.OrdinalIgnoreCase))
                {
                    found = true;
                }
            }
            
            if (!found)
            {
                orphanedPdfs.Add(pdf);
            }
        }
        
        _logger.LogInformation("🔍 Identificados {OrphanedCount} PDFs huérfanos de {TotalCount} generados", 
            orphanedPdfs.Count, _generatedPdfs.Count);
        
        return orphanedPdfs;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray())
            .Replace(" ", "_");
    }
}

/// <summary>
/// Clase para almacenar información del cliente incluyendo su NIT
/// </summary>
public class ClienteInfo
{
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public List<ClienteRow> Rows { get; set; } = new List<ClienteRow>();
}

/// <summary>
/// Clase para almacenar información de una fila de datos del cliente
/// </summary>
public class ClienteRow
{
    public string Numero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public DateTime FechaVence { get; set; }
    public double ValorTotal { get; set; }
    public int NumDias { get; set; }
}
