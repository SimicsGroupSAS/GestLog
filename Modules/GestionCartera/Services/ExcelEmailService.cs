using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionCartera.Services
{
    /// <summary>
    /// Servicio para extraer correos electrónicos desde archivos Excel
    /// Basado en la implementación del proyecto de referencia MiProyectoWPF
    /// </summary>
    public class ExcelEmailService : IExcelEmailService
    {
        private readonly IGestLogLogger _logger;
        private readonly Dictionary<string, string> _nitNormalizadoCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public ExcelEmailService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Dictionary<string, List<string>>> GetEmailsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de correos desde Excel: {ExcelPath}", excelFilePath);

                var resultado = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(excelFilePath) || !File.Exists(excelFilePath))
                {
                    _logger.LogWarning("Archivo Excel no encontrado: {ExcelPath}", excelFilePath);
                    return resultado;
                }

                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook(excelFilePath);
                    
                    foreach (var worksheet in workbook.Worksheets)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Operación cancelada por el usuario");
                            return;
                        }

                        _logger.LogInformation("Procesando hoja: {WorksheetName}", worksheet.Name);
                        ProcessWorksheet(worksheet, resultado);
                    }
                }, cancellationToken);

                _logger.LogInformation("Extracción completada. Se encontraron correos para {CompanyCount} empresas", resultado.Count);
                return resultado;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operación de extracción de correos cancelada");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer correos desde Excel: {ExcelPath}", excelFilePath);
                throw;
            }
        }

        public async Task<List<string>> GetEmailsForCompanyAsync(string excelFilePath, string companyName, string nit, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Buscando correos para empresa: {CompanyName}, NIT: {Nit}", companyName, nit);

                var emails = new List<string>();

                if (string.IsNullOrWhiteSpace(excelFilePath) || !File.Exists(excelFilePath))
                {
                    _logger.LogWarning("Archivo Excel no encontrado: {ExcelPath}", excelFilePath);
                    return emails;
                }

                var nitNormalizado = NormalizeNit(nit);
                var nitSinGuion = nitNormalizado.Replace("-", "");

                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook(excelFilePath);

                    foreach (var worksheet in workbook.Worksheets)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Búsqueda cancelada por el usuario");
                            return;
                        }

                        _logger.LogInformation("Buscando en hoja: {WorksheetName}", worksheet.Name);

                        // Definir columnas según la estructura del proyecto de referencia
                        int tipoDocCol = 2;    // Columna B - Tipo de documento
                        int numIdCol = 3;      // Columna C - Número de identificación  
                        int digitoVerCol = 4;  // Columna D - Dígito de verificación
                        int correoCol = 6;     // Columna F - Email

                        var filas = worksheet.RowsUsed().Skip(1); // Omitir encabezados

                        foreach (var row in filas)
                        {
                            try
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    return;

                                string tipoDoc = row.Cell(tipoDocCol).GetString().Trim();
                                string numId = row.Cell(numIdCol).GetString().Trim();
                                string digitoVer = row.Cell(digitoVerCol).GetString().Trim();
                                string correo = row.Cell(correoCol).GetString().Trim();

                                // Construir NIT completo
                                string nitCompleto = string.Empty;
                                if (tipoDoc.Equals("NIT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(numId))
                                {
                                    nitCompleto = !string.IsNullOrEmpty(digitoVer) ? $"{numId}-{digitoVer}" : numId;
                                }

                                string nitFilaNormalizado = NormalizeNit(nitCompleto);
                                string nitFilaSinGuion = nitFilaNormalizado.Replace("-", "");

                                // Verificar coincidencia por NIT
                                bool coincidePorNIT = !string.IsNullOrEmpty(nitNormalizado) &&
                                                      !string.IsNullOrEmpty(nitFilaNormalizado) &&
                                                      (nitNormalizado.Equals(nitFilaNormalizado, StringComparison.OrdinalIgnoreCase) ||
                                                       nitSinGuion.Equals(nitFilaSinGuion, StringComparison.OrdinalIgnoreCase));

                                if (coincidePorNIT && !string.IsNullOrWhiteSpace(correo))
                                {
                                    // Procesar múltiples correos separados por coma o punto y coma
                                    string[] multipleEmails = correo.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                                    foreach (string email in multipleEmails)
                                    {
                                        string cleanEmail = email.Trim();
                                        if (IsValidEmail(cleanEmail) && !emails.Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                        {
                                            emails.Add(cleanEmail);
                                            _logger.LogInformation("Correo válido encontrado para {CompanyName}: {Email}", companyName, cleanEmail);
                                        }
                                    }
                                }                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error procesando fila en búsqueda de correos para {CompanyName}", companyName);
                            }
                        }

                        // Si ya encontramos correos, no necesitamos buscar en más hojas
                        if (emails.Count > 0)
                            break;
                    }
                }, cancellationToken);

                emails = emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                if (emails.Count == 0)
                {
                    _logger.LogWarning("No se encontraron correos para {CompanyName} con NIT {Nit}", companyName, nit);
                }
                else
                {
                    _logger.LogInformation("Se encontraron {EmailCount} correos para {CompanyName}", emails.Count, companyName);
                }

                return emails;
            }            catch (OperationCanceledException)
            {
                _logger.LogInformation("Búsqueda de correos cancelada para empresa: {CompanyName}", companyName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando correos para empresa: {CompanyName}, NIT: {Nit}", companyName, nit);
                return new List<string>();
            }
        }

        public async Task<(Dictionary<string, List<string>> empresaCorreos, Dictionary<string, List<string>> nitCorreos)> GetEmailMappingsAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Obteniendo mapeos de correos desde Excel: {ExcelPath}", excelFilePath);

                var empresaCorreos = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                var nitCorreos = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(excelFilePath) || !File.Exists(excelFilePath))
                {
                    _logger.LogWarning("Archivo Excel no encontrado: {ExcelPath}", excelFilePath);
                    return (empresaCorreos, nitCorreos);
                }

                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook(excelFilePath);

                    foreach (var worksheet in workbook.Worksheets)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Operación de mapeo cancelada por el usuario");
                            return;
                        }

                        _logger.LogInformation("Procesando mapeos en hoja: {WorksheetName}", worksheet.Name);
                        ProcessWorksheetForMappings(worksheet, empresaCorreos, nitCorreos);
                    }
                }, cancellationToken);

                _logger.LogInformation("Mapeo completado. Empresas: {EmpresaCount}, NITs: {NitCount}", 
                    empresaCorreos.Count, nitCorreos.Count);

                return (empresaCorreos, nitCorreos);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operación de mapeo de correos cancelada");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mapeos de correos desde Excel: {ExcelPath}", excelFilePath);
                return (new Dictionary<string, List<string>>(), new Dictionary<string, List<string>>());
            }
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (email.Contains("@") && email.Contains("."))
            {
                if (email.StartsWith("@") || email.EndsWith("@") ||
                    email.StartsWith(".") || email.EndsWith(".") ||
                    email.Contains("..") || email.Split('@').Length != 2)
                {
                    return false;
                }

                try
                {
                    var addr = new MailAddress(email);
                    return addr.Address == email;
                }
                catch
                {
                    _logger.LogDebug("Correo con formato incorrecto: {Email}", email);
                    return false;
                }
            }

            return false;
        }

        public string NormalizeNit(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return string.Empty;

            // Usar caché para evitar procesar el mismo NIT múltiples veces
            if (_nitNormalizadoCache.TryGetValue(nit, out var cached))
                return cached;

            // Remover espacios, puntos y caracteres especiales, mantener solo números y guiones
            var normalized = new string(nit.Where(c => char.IsDigit(c) || c == '-').ToArray()).Trim();

            _nitNormalizadoCache[nit] = normalized;
            return normalized;
        }

        #region Private Methods

        private void ProcessWorksheet(IXLWorksheet worksheet, Dictionary<string, List<string>> resultado)
        {
            try
            {
                // Definir columnas según estructura estándar
                int tipoDocCol = 2;    // Columna B
                int numIdCol = 3;      // Columna C  
                int digitoVerCol = 4;  // Columna D
                int empresaCol = 5;    // Columna E - Nombre empresa (opcional)
                int correoCol = 6;     // Columna F

                var filas = worksheet.RowsUsed().Skip(1); // Omitir encabezados

                foreach (var row in filas)
                {
                    try
                    {
                        string tipoDoc = row.Cell(tipoDocCol).GetString().Trim();
                        string numId = row.Cell(numIdCol).GetString().Trim();
                        string digitoVer = row.Cell(digitoVerCol).GetString().Trim();
                        string empresa = row.Cell(empresaCol).GetString().Trim();
                        string correo = row.Cell(correoCol).GetString().Trim();

                        // Solo procesar NITs
                        if (!tipoDoc.Equals("NIT", StringComparison.OrdinalIgnoreCase) || 
                            string.IsNullOrEmpty(numId) || 
                            string.IsNullOrWhiteSpace(correo))
                            continue;

                        // Construir NIT completo
                        string nitCompleto = !string.IsNullOrEmpty(digitoVer) ? $"{numId}-{digitoVer}" : numId;
                        string nitNormalizado = NormalizeNit(nitCompleto);

                        // Usar empresa como clave, o NIT si no hay empresa
                        string clave = !string.IsNullOrWhiteSpace(empresa) ? empresa : nitNormalizado;

                        if (string.IsNullOrWhiteSpace(clave))
                            continue;

                        // Procesar múltiples correos
                        string[] multipleEmails = correo.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string email in multipleEmails)
                        {
                            string cleanEmail = email.Trim();
                            if (IsValidEmail(cleanEmail))
                            {
                                if (!resultado.ContainsKey(clave))
                                    resultado[clave] = new List<string>();

                                if (!resultado[clave].Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                {
                                    resultado[clave].Add(cleanEmail);
                                    _logger.LogDebug("Correo agregado para {Clave}: {Email}", clave, cleanEmail);
                                }
                            }
                        }                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando fila en hoja {WorksheetName}", worksheet.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando hoja {WorksheetName}", worksheet.Name);
            }
        }

        private void ProcessWorksheetForMappings(IXLWorksheet worksheet, 
            Dictionary<string, List<string>> empresaCorreos, 
            Dictionary<string, List<string>> nitCorreos)
        {
            try
            {
                int tipoDocCol = 2;
                int numIdCol = 3;
                int digitoVerCol = 4;
                int empresaCol = 5;
                int correoCol = 6;

                var filas = worksheet.RowsUsed().Skip(1);

                foreach (var row in filas)
                {
                    try
                    {
                        string tipoDoc = row.Cell(tipoDocCol).GetString().Trim();
                        string numId = row.Cell(numIdCol).GetString().Trim();
                        string digitoVer = row.Cell(digitoVerCol).GetString().Trim();
                        string empresa = row.Cell(empresaCol).GetString().Trim();
                        string correo = row.Cell(correoCol).GetString().Trim();

                        if (!tipoDoc.Equals("NIT", StringComparison.OrdinalIgnoreCase) || 
                            string.IsNullOrEmpty(numId) || 
                            string.IsNullOrWhiteSpace(correo))
                            continue;

                        string nitCompleto = !string.IsNullOrEmpty(digitoVer) ? $"{numId}-{digitoVer}" : numId;
                        string nitNormalizado = NormalizeNit(nitCompleto);

                        string[] multipleEmails = correo.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string email in multipleEmails)
                        {
                            string cleanEmail = email.Trim();
                            if (IsValidEmail(cleanEmail))
                            {
                                // Agregar a mapeo por empresa
                                if (!string.IsNullOrWhiteSpace(empresa))
                                {
                                    if (!empresaCorreos.ContainsKey(empresa))
                                        empresaCorreos[empresa] = new List<string>();

                                    if (!empresaCorreos[empresa].Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                        empresaCorreos[empresa].Add(cleanEmail);
                                }

                                // Agregar a mapeo por NIT
                                if (!string.IsNullOrWhiteSpace(nitNormalizado))
                                {
                                    if (!nitCorreos.ContainsKey(nitNormalizado))
                                        nitCorreos[nitNormalizado] = new List<string>();

                                    if (!nitCorreos[nitNormalizado].Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                        nitCorreos[nitNormalizado].Add(cleanEmail);
                                }
                            }
                        }                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando fila en mapeo para hoja {WorksheetName}", worksheet.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mapeos en hoja {WorksheetName}", worksheet.Name);
            }
        }

        #endregion
    }
}
