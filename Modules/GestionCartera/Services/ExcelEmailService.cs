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
    /// Servicio optimizado para extraer correos electrónicos desde archivos Excel
    /// Implementa un sistema de índice eficiente para búsquedas O(1)
    /// </summary>
    public class ExcelEmailService : IExcelEmailService
    {
        private readonly IGestLogLogger _logger;
        private readonly Dictionary<string, string> _nitNormalizadoCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Índice para búsquedas eficientes: NIT normalizado -> Lista de emails
        private Dictionary<string, List<string>> _emailIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private string _currentExcelPath = string.Empty;
        private DateTime _lastModified = DateTime.MinValue;

        public ExcelEmailService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Construye un índice en memoria para búsquedas eficientes O(1)
        /// Solo se ejecuta si el archivo cambió desde la última carga
        /// </summary>
        private async Task EnsureEmailIndexLoaded(string excelFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath) || !File.Exists(excelFilePath))
            {
                _logger.LogWarning("Archivo Excel no encontrado para construir índice: {ExcelPath}", excelFilePath ?? "null");
                _emailIndex.Clear();
                return;
            }

            // Verificar si necesitamos recargar el índice
            var fileInfo = new FileInfo(excelFilePath);
            bool needsReload = _currentExcelPath != excelFilePath || 
                              _lastModified != fileInfo.LastWriteTime || 
                              _emailIndex.Count == 0;

            if (!needsReload)
            {
                _logger.LogDebug("Índice de emails ya está actualizado para: {ExcelPath}", excelFilePath);
                return;
            }

            _logger.LogInformation("🔄 Construyendo índice de emails desde: {ExcelPath}", excelFilePath);
            var startTime = DateTime.Now;

            try
            {
                _emailIndex.Clear();
                var tempIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook(excelFilePath);

                    foreach (var worksheet in workbook.Worksheets)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Construcción de índice cancelada por el usuario");
                            return;
                        }

                        _logger.LogDebug("Indexando hoja: {WorksheetName}", worksheet.Name);
                        ProcessWorksheetForIndex(worksheet, tempIndex);
                    }
                }, cancellationToken);

                // Solo actualizar si completamos exitosamente
                if (!cancellationToken.IsCancellationRequested)
                {
                    _emailIndex = tempIndex;
                    _currentExcelPath = excelFilePath;
                    _lastModified = fileInfo.LastWriteTime;

                    var elapsed = DateTime.Now - startTime;
                    _logger.LogInformation("✅ Índice construido exitosamente: {Count} NITs indexados en {ElapsedMs}ms", 
                        _emailIndex.Count, elapsed.TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error construyendo índice de emails");
                _emailIndex.Clear();
                throw;
            }
        }

        /// <summary>
        /// Procesa una hoja de Excel para construir el índice NIT->Emails
        /// </summary>
        private void ProcessWorksheetForIndex(IXLWorksheet worksheet, Dictionary<string, List<string>> index)
        {
            try
            {
                int tipoDocCol = 2;    // Columna B - Tipo de documento
                int numIdCol = 3;      // Columna C - Número de identificación  
                int digitoVerCol = 4;  // Columna D - Dígito de verificación
                int correoCol = 6;     // Columna F - Email

                var filas = worksheet.RowsUsed().Skip(1); // Omitir encabezados

                foreach (var row in filas)
                {
                    try
                    {
                        string tipoDoc = row.Cell(tipoDocCol).GetString().Trim();
                        string numId = row.Cell(numIdCol).GetString().Trim();
                        string digitoVer = row.Cell(digitoVerCol).GetString().Trim();
                        string correo = row.Cell(correoCol).GetString().Trim();

                        // Solo procesar NITs con emails válidos
                        if (!tipoDoc.Equals("NIT", StringComparison.OrdinalIgnoreCase) || 
                            string.IsNullOrEmpty(numId) || 
                            string.IsNullOrWhiteSpace(correo))
                            continue;

                        // Construir NIT completo y normalizarlo
                        string nitCompleto = !string.IsNullOrEmpty(digitoVer) ? $"{numId}-{digitoVer}" : numId;
                        string nitNormalizado = NormalizeNit(nitCompleto);

                        if (string.IsNullOrEmpty(nitNormalizado))
                            continue;

                        // Procesar múltiples correos separados por coma o punto y coma
                        string[] multipleEmails = correo.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string email in multipleEmails)
                        {
                            string emailLimpio = email.Trim();
                            if (IsValidEmail(emailLimpio))
                            {
                                if (!index.ContainsKey(nitNormalizado))
                                {
                                    index[nitNormalizado] = new List<string>();
                                }

                                if (!index[nitNormalizado].Contains(emailLimpio, StringComparer.OrdinalIgnoreCase))
                                {
                                    index[nitNormalizado].Add(emailLimpio);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando fila en índice para hoja {WorksheetName}", worksheet.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando hoja para índice {WorksheetName}", worksheet.Name);
            }
        }

        /// <summary>
        /// Versión optimizada: Búsqueda O(1) usando índice en memoria
        /// </summary>
        public async Task<List<string>> GetEmailsForCompanyAsync(string excelFilePath, string companyName, string nit, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔍 Buscando correos para empresa: {CompanyName}, NIT: {Nit}", companyName, nit);

                // Normalizar NIT para búsqueda
                var nitNormalizado = NormalizeNit(nit ?? string.Empty);
                
                if (string.IsNullOrEmpty(nitNormalizado))
                {
                    _logger.LogWarning("NIT inválido o vacío para empresa: {CompanyName}", companyName);
                    return new List<string>();
                }

                // Asegurar que el índice esté cargado (solo se ejecuta si es necesario)
                await EnsureEmailIndexLoaded(excelFilePath, cancellationToken);

                // Búsqueda O(1) en el índice
                if (_emailIndex.TryGetValue(nitNormalizado, out var emails))
                {
                    _logger.LogInformation("✅ Se encontraron {EmailCount} correos para {CompanyName} (NIT: {Nit})", 
                        emails.Count, companyName, nitNormalizado);
                    return new List<string>(emails); // Retornar copia para evitar modificaciones externas
                }
                else
                {
                    _logger.LogWarning("❌ No se encontraron correos para {CompanyName} con NIT {Nit}", companyName, nitNormalizado);
                    return new List<string>();
                }
            }
            catch (OperationCanceledException)
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

        public async Task<Dictionary<string, List<string>>> GetEmailsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de correos desde Excel: {ExcelPath}", excelFilePath);

                // Usar el índice para generar el diccionario por empresa
                await EnsureEmailIndexLoaded(excelFilePath, cancellationToken);

                var resultado = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                // Convertir índice NIT->Email a Empresa->Email si es necesario
                // Para simplificar, retornamos el índice por NIT
                foreach (var kvp in _emailIndex)
                {
                    resultado[kvp.Key] = new List<string>(kvp.Value);
                }

                _logger.LogInformation("Extracción completada. Se encontraron correos para {CompanyCount} NITs", resultado.Count);
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

        public async Task<(Dictionary<string, List<string>> empresaCorreos, Dictionary<string, List<string>> nitCorreos)> GetEmailMappingsAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Obteniendo mapeos de correos desde Excel: {ExcelPath}", excelFilePath);

                await EnsureEmailIndexLoaded(excelFilePath, cancellationToken);

                var empresaCorreos = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                var nitCorreos = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                // El índice ya contiene NITs -> Emails
                foreach (var kvp in _emailIndex)
                {
                    nitCorreos[kvp.Key] = new List<string>(kvp.Value);
                }

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

            // Remover prefijo "NIT" si existe y normalizar espacios
            string cleanNit = nit.Trim();
            if (cleanNit.StartsWith("NIT", StringComparison.OrdinalIgnoreCase))
            {
                cleanNit = cleanNit.Substring(3).Trim();
            }

            // Remover espacios, puntos y caracteres especiales, mantener solo números y guiones
            var normalized = new string(cleanNit.Where(c => char.IsDigit(c) || c == '-').ToArray()).Trim();

            _nitNormalizadoCache[nit] = normalized;
            return normalized;
        }

        #region Legacy Methods (mantener compatibilidad)

        private void ProcessWorksheet(IXLWorksheet worksheet, Dictionary<string, List<string>> resultado)
        {
            // Implementación legacy mantenida para compatibilidad
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
                        string clave = !string.IsNullOrWhiteSpace(empresa) ? empresa : nitNormalizado;

                        if (string.IsNullOrWhiteSpace(clave))
                            continue;

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
                                }
                            }
                        }
                    }
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
            // Implementación legacy mantenida para compatibilidad
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
                                if (!string.IsNullOrWhiteSpace(empresa))
                                {
                                    if (!empresaCorreos.ContainsKey(empresa))
                                        empresaCorreos[empresa] = new List<string>();

                                    if (!empresaCorreos[empresa].Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                        empresaCorreos[empresa].Add(cleanEmail);
                                }

                                if (!string.IsNullOrWhiteSpace(nitNormalizado))
                                {
                                    if (!nitCorreos.ContainsKey(nitNormalizado))
                                        nitCorreos[nitNormalizado] = new List<string>();

                                    if (!nitCorreos[nitNormalizado].Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                        nitCorreos[nitNormalizado].Add(cleanEmail);
                                }
                            }
                        }
                    }
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
