using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ClosedXML.Excel;
using GestLog.Modules.DaaterProccesor.Services;
using GestLog.Services;

namespace GestLog.Views.Tools.DaaterProccesor
{
    public partial class FilteredDataView : Window
    {
        private DataTable _originalTable = new DataTable();
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly IGestLogLogger _logger;

    public FilteredDataView()
    {
        InitializeComponent();
        _logger = LoggingService.GetLogger<FilteredDataView>();
        _logger.LogDebug("🔍 Iniciando FilteredDataView...");
        _ = LoadDataAsync(); // Fire and forget para el constructor
    }private async Task LoadDataAsync()
        {
            using var scope = _logger.BeginScope("LoadDataAsync");
            _logger.LogDebug("🔍 Iniciando carga de datos consolidados...");
            
            try
            {
                // Selección automática del archivo consolidado más reciente en la carpeta Output
                var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                DataTable? dt = null;
                
                _logger.LogDebug("📁 Buscando archivos en directorio: {OutputDir}", outputDir);
                
                // Verificar si la carpeta Output existe antes de buscar archivos
                if (Directory.Exists(outputDir))
                {
                    var files = Directory.GetFiles(outputDir, "*Consolidado*.xlsx");
                    var file = files.OrderByDescending(f => File.GetLastWriteTime(f)).FirstOrDefault();
                    if (file != null)
                    {
                        _logger.LogInformation("📄 Archivo consolidado encontrado: {FileName}", Path.GetFileName(file));
                        dt = await LoadConsolidatedExcelAsync(file);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No se encontraron archivos consolidados en {OutputDir}", outputDir);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Directorio Output no existe: {OutputDir}", outputDir);
                }
                
                if (dt != null)
                {                    _logger.LogDebug("🔧 Aplicando filtros a {RowCount} registros...", dt.Rows.Count);
                    var filterService = new ConsolidatedFilterService(_logger);
                    var filtered = filterService.FilterRows(dt);
                    
                    Dispatcher.Invoke(() =>
                    {                        try
                        {
                            // Actualizar interfaz de usuario (controles definidos en XAML)
                            _originalTable = filtered;
                            // UpdateRecordCount(filtered.Rows.Count);
                            // Otros controles se actualizarán cuando estén disponibles
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error actualizando interfaz de usuario");
                        }
                    });
                    
                    _logger.LogInformation("✅ Datos cargados correctamente: {FilteredCount} registros filtrados de {TotalCount} originales", 
                        filtered.Rows.Count, dt.Rows.Count);
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        _originalTable = new DataTable();
                        // UpdateRecordCount(0);
                    });
                    
                    _logger.LogWarning("⚠️ No se pudieron cargar datos consolidados");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al cargar datos consolidados");
                
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error al cargar los datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void UpdateRecordCount(int count)
        {
            txtRecordCount.Text = $"Registros: {count:N0}";
        }        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogUserInteraction("🔍", "ApplyFilters", "Usuario aplicó filtros manualmente");
            
            try
            {                var filterService = new ConsolidatedFilterService(_logger);
                var filtered = filterService.FilterRows(_originalTable);
                
                _logger.LogInformation("🔧 Filtros aplicados: {FilteredCount} registros de {TotalCount} originales", 
                    filtered.Rows.Count, _originalTable.Rows.Count);
                
                // Comentado hasta que los controles XAML estén disponibles
                // FilteredDataGrid.ItemsSource = filtered.DefaultView;
                // UpdateRecordCount(filtered.Rows.Count);
                // btnExportExcel.IsEnabled = filtered.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error aplicando filtros");
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        private async void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogUserInteraction("📤", "ExportToExcel", "Usuario inició exportación a Excel");
            
            try
            {
                // Comentado hasta que los controles XAML estén disponibles
                // var filteredData = FilteredDataGrid.ItemsSource as DataView;
                // if (filteredData == null || filteredData.Count == 0)
                // {
                //     MessageBox.Show("No hay datos filtrados para exportar.", "Sin datos", MessageBoxButton.OK, MessageBoxImage.Information);
                //     return;
                // }
                  if (_originalTable == null || _originalTable.Rows.Count == 0)
                {
                    _logger.LogWarning("⚠️ No hay datos para exportar");
                    MessageBox.Show("No hay datos filtrados para exportar.", "Sin datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dataRowCount = _originalTable.Rows.Count;
                _logger.LogInformation("📊 Preparando exportación de {RowCount} registros", dataRowCount);

                var result = MessageBox.Show(
                    $"¿Desea generar un archivo Excel con los {dataRowCount:N0} registros filtrados?\n\n" +
                    "Este archivo contendrá únicamente los productos de acero y perfiles metálicos que cumplen con los criterios de filtrado.",
                    "Exportar datos filtrados",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // btnExportExcel.IsEnabled = false; // Comentado hasta que el control esté disponible
                    try
                    {
                        await ExportFilteredDataToExcelAsync(_originalTable);
                        _logger.LogInformation("✅ Exportación completada exitosamente");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error durante la exportación a Excel");
                        throw;
                    }
                    finally
                    {
                        // btnExportExcel.IsEnabled = true; // Comentado hasta que el control esté disponible
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en proceso de exportación");
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        private async Task ExportFilteredDataToExcelAsync(DataTable data)
        {
            using var scope = _logger.BeginScope("ExportFilteredDataToExcel");
            _logger.LogInformation("📤 Iniciando exportación de {RowCount} registros a Excel", data.Rows.Count);
            
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                    FileName = $"DatosFiltrados_Acero_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Guardar datos filtrados"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _logger.LogDebug("📁 Archivo seleccionado para exportación: {FileName}", saveFileDialog.FileName);
                    _cancellationTokenSource = new CancellationTokenSource();
                      await Task.Run(() =>
                    {
                        _logger.LogDebug("📊 Iniciando generación de Excel con {RowCount} filas y {ColumnCount} columnas", 
                            data.Rows.Count, data.Columns.Count);
                        
                        _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Datos Filtrados");
                            
                            // Agregar encabezados
                            _logger.LogDebug("📝 Agregando encabezados a la hoja Excel");
                            for (int col = 0; col < data.Columns.Count; col++)
                            {
                                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                worksheet.Cell(1, col + 1).Value = data.Columns[col].ColumnName;
                                worksheet.Cell(1, col + 1).Style.Font.Bold = true;
                                worksheet.Cell(1, col + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                            }

                            // Agregar datos
                            _logger.LogDebug("📋 Agregando {RowCount} filas de datos", data.Rows.Count);
                            for (int row = 0; row < data.Rows.Count; row++)
                            {
                                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                for (int col = 0; col < data.Columns.Count; col++)
                                {
                                    worksheet.Cell(row + 2, col + 1).Value = data.Rows[row][col]?.ToString() ?? "";
                                }
                            }                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            
                            // Ajustar ancho de columnas
                            _logger.LogDebug("🎨 Aplicando formato y ajustando columnas");
                            worksheet.ColumnsUsed().AdjustToContents();

                            // Aplicar filtros automáticos
                            var range = worksheet.Range(1, 1, data.Rows.Count + 1, data.Columns.Count);
                            range.SetAutoFilter();

                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            _logger.LogDebug("💾 Guardando archivo Excel: {FileName}", saveFileDialog.FileName);
                            workbook.SaveAs(saveFileDialog.FileName);
                        }
                    }, _cancellationTokenSource.Token);

                    _logger.LogInformation("✅ Archivo Excel exportado exitosamente: {FileName}", saveFileDialog.FileName);

                    var openResult = MessageBox.Show(
                        $"Archivo exportado exitosamente:\n{saveFileDialog.FileName}\n\n¿Desea abrir el archivo ahora?",
                        "Exportación exitosa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openResult == MessageBoxResult.Yes)
                    {
                        _logger.LogUserInteraction("🔗", "OpenExportedFile", "Usuario abrió archivo exportado: {FileName}", saveFileDialog.FileName);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    _logger.LogInformation("❌ Usuario canceló la selección de archivo para exportación");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⏹️ Exportación cancelada por el usuario");
                MessageBox.Show("Exportación cancelada por el usuario.", "Cancelado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al exportar el archivo Excel");
                MessageBox.Show($"Error al exportar el archivo:\n{ex.Message}", "Error de exportación", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }        private async Task<DataTable?> LoadConsolidatedExcelAsync(string filePath)
        {
            using var scope = _logger.BeginScope("LoadConsolidatedExcelAsync");
            _logger.LogInformation("📂 Iniciando carga de archivo Excel consolidado: {FilePath}", filePath);
            
            return await Task.Run(() =>
            {
                try
                {
                    var dt = new DataTable();
                    var fileInfo = new FileInfo(filePath);
                    _logger.LogDebug("📊 Información del archivo: {FileName} - Tamaño: {FileSize:N0} bytes", 
                        fileInfo.Name, fileInfo.Length);
                    
                    using (var workbook = new XLWorkbook(filePath))
                    {
                        _logger.LogDebug("📋 Archivo Excel abierto correctamente");
                        var worksheet = workbook.Worksheets.Cast<IXLWorksheet>().FirstOrDefault();
                        
                        if (worksheet == null) 
                        {
                            _logger.LogWarning("⚠️ No se encontraron hojas de trabajo en el archivo Excel");
                            return null;
                        }
                        
                        _logger.LogDebug("📄 Procesando hoja de trabajo: {WorksheetName}", worksheet.Name);
                        var usedRows = worksheet.RowsUsed().ToList();
                        _logger.LogDebug("📊 Filas utilizadas encontradas: {RowCount}", usedRows.Count);
                        
                        bool firstRow = true;
                        int processedRows = 0;
                        
                        foreach (var row in usedRows)
                        {
                            if (firstRow)
                            {
                                // Procesar encabezados
                                _logger.LogDebug("📝 Procesando encabezados...");
                                var headers = row.Cells().Select(c => c.GetString()).ToList();
                                foreach (var header in headers)
                                {
                                    dt.Columns.Add(header);
                                }
                                _logger.LogDebug("✅ Encabezados procesados: {ColumnCount} columnas", dt.Columns.Count);
                                firstRow = false;
                            }
                            else
                            {
                                // Procesar datos
                                var values = row.Cells(1, dt.Columns.Count).Select(c => c.GetValue<string>()).ToArray();
                                dt.Rows.Add(values);
                                processedRows++;
                                
                                // Log de progreso cada 1000 filas
                                if (processedRows % 1000 == 0)
                                {
                                    _logger.LogDebug("🔄 Progreso de carga: {ProcessedRows} filas procesadas", processedRows);
                                }
                            }
                        }
                        
                        _logger.LogInformation("✅ Carga completada exitosamente: {TotalRows} filas de datos, {ColumnCount} columnas", 
                            processedRows, dt.Columns.Count);
                    }
                    
                    return dt;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al cargar archivo Excel consolidado: {FilePath}", filePath);
                    throw;
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            base.OnClosed(e);
        }
    }
}
