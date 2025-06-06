using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Error;
using GestLog.Services.Core.Logging;

namespace GestLog.ViewModels
{
    /// <summary>
    /// ViewModel para mostrar y gestionar el registro de errores de la aplicación
    /// </summary>
    public partial class ErrorLogViewModel : ObservableObject
    {
        private readonly IErrorHandlingService _errorHandler;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private ObservableCollection<ErrorRecord> errors;

        [ObservableProperty]
        private ErrorRecord? selectedError;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public ErrorLogViewModel()
        {
            _errorHandler = LoggingService.GetErrorHandler();
            _logger = LoggingService.GetLogger();
            errors = new ObservableCollection<ErrorRecord>();

            // Suscribirse al evento de errores
            _errorHandler.ErrorOccurred += (sender, e) =>
            {
                App.Current.Dispatcher.Invoke(() => 
                {
                    // Insertar el nuevo error al inicio de la colección
                    Errors.Insert(0, e.Error);
                    StatusMessage = $"Nuevo error recibido: {e.Error.ExceptionType} en {e.Error.Context}";
                });
            };

            // Cargar errores iniciales
            RefreshErrorLogCommand.Execute(null);
        }

        [RelayCommand]
        private async Task RefreshErrorLog()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Cargando registro de errores...";

                await Task.Run(() =>
                {
                    var recentErrors = _errorHandler.GetRecentErrors();
                    
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Errors.Clear();
                        foreach (var error in recentErrors)
                        {
                            Errors.Add(error);
                        }
                    });
                });

                StatusMessage = $"Se cargaron {Errors.Count} registros de error";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar registro de errores");
                StatusMessage = "Error al cargar los registros";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedError = null;
        }

        [RelayCommand]
        private void CopyErrorDetails()
        {
            if (SelectedError == null)
                return;

            var details = $"Error ID: {SelectedError.Id}\n" +
                          $"Fecha: {SelectedError.Timestamp}\n" +
                          $"Contexto: {SelectedError.Context}\n" +
                          $"Tipo: {SelectedError.ExceptionType}\n" +
                          $"Mensaje: {SelectedError.Message}\n\n" +
                          $"Stack Trace:\n{SelectedError.StackTrace}";

            try
            {
                System.Windows.Clipboard.SetText(details);
                StatusMessage = "Detalles de error copiados al portapapeles";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al copiar detalles al portapapeles");
                StatusMessage = "Error al copiar detalles al portapapeles";
            }
        }
    }
}
