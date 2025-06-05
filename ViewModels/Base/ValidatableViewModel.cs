using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using GestLog.Models.Validation;
using GestLog.Services;

// Usar alias para evitar el conflicto de tipos
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace GestLog.ViewModels.Base
{
    /// <summary>
    /// Clase base para ViewModels que requieren validación de datos
    /// Implementa INotifyDataErrorInfo para integración con WPF
    /// </summary>
    public abstract class ValidatableViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();
        private readonly IValidationService _validationService;

        protected ValidatableViewModel(IValidationService? validationService = null)
        {
            _validationService = validationService ?? new ValidationService(LoggingService.GetLogger());
            
            // Inicializar eventos para evitar problemas de nulabilidad
            PropertyChanged = delegate { };
            ErrorsChanged = delegate { };
        }

        #region INotifyPropertyChanged

        // Hacerlo anulable para coincidir con la interfaz
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            ValidateProperty(value, propertyName);
            return true;
        }

        #endregion

        #region INotifyDataErrorInfo

        // Hacerlo anulable para coincidir con la interfaz
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public bool HasErrors => _errors.Any(x => x.Value?.Any() == true);

        // Hacerlo anulable para coincidir con la interfaz
        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                // Retorna todos los errores
                return _errors.SelectMany(x => x.Value).ToList();
            }

            return _errors.ContainsKey(propertyName) ? _errors[propertyName] : Enumerable.Empty<string>();
        }

        #endregion

        #region Validación

        /// <summary>
        /// Valida una propiedad específica
        /// </summary>
        protected virtual void ValidateProperty(object? value, [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                return;

            var results = new List<DataAnnotationsValidationResult>();
            var context = new ValidationContext(this) { MemberName = propertyName };

            // Validación usando DataAnnotations
            if (value != null)
            {
                Validator.TryValidateProperty(value, context, results);
            }

            // Validación personalizada usando el servicio
            var customValidationResult = _validationService.ValidateProperty(this, propertyName, value);
            
            // Convertir errores del servicio de validación a DataAnnotationsValidationResult
            if (!customValidationResult.IsValid)
            {
                foreach (var error in customValidationResult.Errors)
                {
                    if (!string.IsNullOrEmpty(error.Message))
                    {
                        results.Add(new DataAnnotationsValidationResult(error.Message, new[] { propertyName }));
                    }
                }
            }

            // Actualizar errores
            var errorMessages = new List<string>();
            foreach (var result in results)
            {
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    errorMessages.Add(result.ErrorMessage);
                }
            }
            
            UpdateErrors(propertyName, errorMessages);
        }

        /// <summary>
        /// Valida todo el objeto
        /// </summary>
        public virtual bool ValidateAll()
        {
            // Limpiar errores existentes
            var propertiesToValidate = _errors.Keys.ToList();
            foreach (var property in propertiesToValidate)
            {
                _errors[property].Clear();
                OnErrorsChanged(property);
            }

            // Validar usando DataAnnotations
            var context = new ValidationContext(this);
            var results = new List<DataAnnotationsValidationResult>();
            Validator.TryValidateObject(this, context, results, true);

            // Agrupar errores por propiedad
            foreach (var result in results)
            {
                foreach (var memberName in result.MemberNames)
                {
                    if (!string.IsNullOrEmpty(result.ErrorMessage) && !string.IsNullOrEmpty(memberName))
                    {
                        UpdateErrors(memberName, new List<string> { result.ErrorMessage });
                    }
                }
            }

            // Validación personalizada usando el servicio
            var serviceValidationResult = _validationService.ValidateObject(this);
            if (!serviceValidationResult.IsValid)
            {
                foreach (var error in serviceValidationResult.Errors)
                {
                    var propertyName = error.PropertyName ?? "General";
                    if (!string.IsNullOrEmpty(error.Message))
                    {
                        UpdateErrors(propertyName, new List<string> { error.Message });
                    }
                }
            }

            return !HasErrors;
        }

        /// <summary>
        /// Actualiza los errores para una propiedad específica
        /// </summary>
        private void UpdateErrors(string propertyName, List<string> newErrors)
        {
            var hasErrors = newErrors.Count > 0;

            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            var currentErrors = _errors[propertyName];
            currentErrors.Clear();

            if (hasErrors)
                currentErrors.AddRange(newErrors);

            OnErrorsChanged(propertyName);
        }

        /// <summary>
        /// Notifica cambios en los errores
        /// </summary>
        protected virtual void OnErrorsChanged([CallerMemberName] string? propertyName = null)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName ?? string.Empty));
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Limpia todos los errores
        /// </summary>
        public void ClearAllErrors()
        {
            var propertiesToClear = _errors.Keys.ToList();
            foreach (var property in propertiesToClear)
            {
                _errors[property].Clear();
                OnErrorsChanged(property);
            }
        }

        /// <summary>
        /// Obtiene todos los errores como una lista plana
        /// </summary>
        public List<string> GetAllErrors()
        {
            return _errors.SelectMany(x => x.Value).ToList();
        }

        /// <summary>
        /// Verifica si una propiedad específica tiene errores
        /// </summary>
        public bool HasErrorsForProperty(string propertyName)
        {
            return _errors.ContainsKey(propertyName) && _errors[propertyName].Any();
        }

        #endregion

        #region Métodos de validación personalizados

        /// <summary>
        /// Permite agregar validación personalizada en clases derivadas
        /// </summary>
        protected virtual GestLog.Models.Validation.ValidationResult CustomValidation()
        {
            return GestLog.Models.Validation.ValidationResult.Success();
        }

        /// <summary>
        /// Registra un validador personalizado para este ViewModel
        /// </summary>
        protected void RegisterCustomValidator<T>(IValidator<T> validator) where T : class
        {
            _validationService.RegisterValidator(validator);
        }

        #endregion
    }
}
