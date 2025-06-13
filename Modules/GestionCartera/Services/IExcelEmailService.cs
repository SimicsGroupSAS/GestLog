using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace GestLog.Modules.GestionCartera.Services
{
    /// <summary>
    /// Servicio para extraer información de correos electrónicos desde archivos Excel
    /// </summary>
    public interface IExcelEmailService
    {
        /// <summary>
        /// Extrae correos electrónicos desde un archivo Excel basándose en NITs y nombres de empresas
        /// </summary>
        /// <param name="excelFilePath">Ruta del archivo Excel</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Diccionario con empresa como clave y lista de correos como valor</returns>
        Task<Dictionary<string, List<string>>> GetEmailsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene correos para una empresa específica basándose en su nombre y NIT
        /// </summary>
        /// <param name="excelFilePath">Ruta del archivo Excel</param>
        /// <param name="companyName">Nombre de la empresa</param>
        /// <param name="nit">NIT de la empresa</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de correos encontrados para la empresa</returns>
        Task<List<string>> GetEmailsForCompanyAsync(string excelFilePath, string companyName, string nit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la información de empresa-correo mapeada por NIT y nombre
        /// </summary>
        /// <param name="excelFilePath">Ruta del archivo Excel</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Tupla con diccionarios de empresa->correos y nit->correos</returns>
        Task<(Dictionary<string, List<string>> empresaCorreos, Dictionary<string, List<string>> nitCorreos)> GetEmailMappingsAsync(string excelFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida si una dirección de correo electrónico es válida
        /// </summary>
        /// <param name="email">Dirección de correo a validar</param>
        /// <returns>True si es válida, false en caso contrario</returns>
        bool IsValidEmail(string email);

        /// <summary>
        /// Normaliza un NIT eliminando caracteres especiales y espacios
        /// </summary>
        /// <param name="nit">NIT a normalizar</param>
        /// <returns>NIT normalizado</returns>
        string NormalizeNit(string nit);

        /// <summary>
        /// Valida la estructura del archivo Excel de correos antes de procesarlo
        /// </summary>
        /// <param name="excelFilePath">Ruta del archivo Excel</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si el archivo tiene la estructura correcta</returns>
        /// <exception cref="EmailExcelValidationException">Si el archivo no es válido</exception>
        /// <exception cref="EmailExcelStructureException">Si faltan columnas requeridas</exception>
        Task<bool> ValidateExcelStructureAsync(string excelFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene información de validación del archivo Excel sin lanzar excepciones
        /// </summary>
        /// <param name="excelFilePath">Ruta del archivo Excel</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de validación con detalles</returns>
        Task<ExcelValidationResult> GetValidationInfoAsync(string excelFilePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Resultado de validación de archivo Excel de correos
    /// </summary>
    public class ExcelValidationResult
    {
        public bool IsValid { get; init; }
        public string Message { get; init; } = string.Empty;
        public string[] RequiredColumns { get; init; } = Array.Empty<string>();
        public string[] FoundColumns { get; init; } = Array.Empty<string>();
        public string[] MissingColumns { get; init; } = Array.Empty<string>();
        public int TotalRows { get; init; }
        public int ValidEmailRows { get; init; }
        public int ValidNitRows { get; init; }
        public string[] SampleEmails { get; init; } = Array.Empty<string>();

        public static ExcelValidationResult Valid(string message, string[] foundColumns, int totalRows, int validEmailRows, int validNitRows, string[] sampleEmails)
        {
            return new ExcelValidationResult
            {
                IsValid = true,
                Message = message,
                FoundColumns = foundColumns,
                RequiredColumns = new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                MissingColumns = Array.Empty<string>(),
                TotalRows = totalRows,
                ValidEmailRows = validEmailRows,
                ValidNitRows = validNitRows,
                SampleEmails = sampleEmails
            };
        }

        public static ExcelValidationResult Invalid(string message, string[] requiredColumns, string[] foundColumns, string[] missingColumns)
        {
            return new ExcelValidationResult
            {
                IsValid = false,
                Message = message,
                RequiredColumns = requiredColumns,
                FoundColumns = foundColumns,
                MissingColumns = missingColumns,
                TotalRows = 0,
                ValidEmailRows = 0,
                ValidNitRows = 0,
                SampleEmails = Array.Empty<string>()
            };
        }
    }
}
