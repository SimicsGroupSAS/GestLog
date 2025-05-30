using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ClosedXML.Excel;
using GestLog.Modules.DaaterProccesor.Services;

namespace GestLog.Views.Tools.DaaterProccesor
{
    public partial class FilteredDataView : Window
    {
        private DataTable _originalTable = new DataTable();

        public FilteredDataView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            // Selección automática del archivo consolidado más reciente en la carpeta Output
            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            DataTable? dt = null;
            
            // Verificar si la carpeta Output existe antes de buscar archivos
            if (Directory.Exists(outputDir))
            {
                var files = Directory.GetFiles(outputDir, "*Consolidado*.xlsx");
                var file = files.OrderByDescending(f => File.GetLastWriteTime(f)).FirstOrDefault();
                if (file != null)
                {
                    dt = LoadConsolidatedExcel(file);
                }
            }
            
            if (dt != null)
            {
                var filterService = new ConsolidatedFilterService();
                var filtered = filterService.FilterRows(dt);
                FilteredDataGrid.ItemsSource = filtered.DefaultView;
                _originalTable = filtered;
                UpdateRecordCount(filtered.Rows.Count);
                btnExportExcel.IsEnabled = filtered.Rows.Count > 0;
            }
            else
            {
                _originalTable = new DataTable();
                UpdateRecordCount(0);
            }
        }

        private void UpdateRecordCount(int count)
        {
            txtRecordCount.Text = $"Registros: {count:N0}";
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            var filterService = new ConsolidatedFilterService();
            var filtered = filterService.FilterRows(_originalTable);
            FilteredDataGrid.ItemsSource = filtered.DefaultView;
            UpdateRecordCount(filtered.Rows.Count);
            btnExportExcel.IsEnabled = filtered.Rows.Count > 0;
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var filteredData = FilteredDataGrid.ItemsSource as DataView;
            if (filteredData == null || filteredData.Count == 0)
            {
                MessageBox.Show("No hay datos filtrados para exportar.", "Sin datos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"¿Desea generar un archivo Excel con los {filteredData.Count:N0} registros filtrados?\n\n" +
                "Este archivo contendrá únicamente los productos de acero y perfiles metálicos que cumplen con los criterios de filtrado.",
                "Exportar datos filtrados",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ExportFilteredDataToExcel(filteredData.ToTable());
            }
        }

        private void ExportFilteredDataToExcel(DataTable data)
        {
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
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Datos Filtrados");
                        
                        // Agregar encabezados
                        for (int col = 0; col < data.Columns.Count; col++)
                        {
                            worksheet.Cell(1, col + 1).Value = data.Columns[col].ColumnName;
                            worksheet.Cell(1, col + 1).Style.Font.Bold = true;
                            worksheet.Cell(1, col + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                        }

                        // Agregar datos
                        for (int row = 0; row < data.Rows.Count; row++)
                        {
                            for (int col = 0; col < data.Columns.Count; col++)
                            {
                                worksheet.Cell(row + 2, col + 1).Value = data.Rows[row][col]?.ToString() ?? "";
                            }
                        }

                        // Ajustar ancho de columnas
                        worksheet.ColumnsUsed().AdjustToContents();

                        // Aplicar filtros automáticos
                        var range = worksheet.Range(1, 1, data.Rows.Count + 1, data.Columns.Count);
                        range.SetAutoFilter();

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    var openResult = MessageBox.Show(
                        $"Archivo exportado exitosamente:\n{saveFileDialog.FileName}\n\n¿Desea abrir el archivo ahora?",
                        "Exportación exitosa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openResult == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar el archivo:\n{ex.Message}", "Error de exportación", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable? LoadConsolidatedExcel(string filePath)
        {
            var dt = new DataTable();
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.Cast<IXLWorksheet>().FirstOrDefault();
                if (worksheet == null) return null;
                
                bool firstRow = true;
                foreach (var row in worksheet.RowsUsed())
                {
                    if (firstRow)
                    {
                        foreach (var cell in row.Cells())
                            dt.Columns.Add(cell.GetString());
                        firstRow = false;
                    }
                    else
                    {
                        var values = row.Cells(1, dt.Columns.Count).Select(c => c.GetValue<string>()).ToArray();
                        dt.Rows.Add(values);
                    }
                }
            }
            return dt;
        }
    }
}
