using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Collections;

// Evitamos cualquier posibilidad de ambigüedad con System.Windows.Forms
namespace GestLog.Controls.Validation
{
    /// <summary>
    /// Control TextBox personalizado que integra validación visual
    /// </summary>
    public class ValidatingTextBox : System.Windows.Controls.TextBox
    {
        static ValidatingTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ValidatingTextBox), 
                new FrameworkPropertyMetadata(typeof(ValidatingTextBox)));
        }

        public ValidatingTextBox()
        {
            // Suscribirse a cambios de validación
            System.Windows.Controls.Validation.AddErrorHandler(this, OnValidationError);
        }

        #region Dependency Properties

        /// <summary>
        /// Propiedad que indica si el control tiene errores de validación
        /// </summary>
        public static readonly DependencyProperty HasValidationErrorProperty =
            DependencyProperty.Register(nameof(HasValidationError), typeof(bool), typeof(ValidatingTextBox),
                new PropertyMetadata(false));

        public bool HasValidationError
        {
            get => (bool)GetValue(HasValidationErrorProperty);
            set => SetValue(HasValidationErrorProperty, value);
        }

        /// <summary>
        /// Mensaje de error de validación
        /// </summary>
        public static readonly DependencyProperty ValidationErrorMessageProperty =
            DependencyProperty.Register(nameof(ValidationErrorMessage), typeof(string), typeof(ValidatingTextBox),
                new PropertyMetadata(string.Empty));

        public string ValidationErrorMessage
        {
            get => (string)GetValue(ValidationErrorMessageProperty);
            set => SetValue(ValidationErrorMessageProperty, value);
        }

        /// <summary>
        /// Color del borde cuando hay error
        /// </summary>
        public static readonly DependencyProperty ErrorBorderBrushProperty =
            DependencyProperty.Register(nameof(ErrorBorderBrush), typeof(System.Windows.Media.Brush), typeof(ValidatingTextBox),
                new PropertyMetadata(System.Windows.Media.Brushes.Red));

        public System.Windows.Media.Brush ErrorBorderBrush
        {
            get => (System.Windows.Media.Brush)GetValue(ErrorBorderBrushProperty);
            set => SetValue(ErrorBorderBrushProperty, value);
        }

        /// <summary>
        /// Grosor del borde cuando hay error
        /// </summary>
        public static readonly DependencyProperty ErrorBorderThicknessProperty =
            DependencyProperty.Register(nameof(ErrorBorderThickness), typeof(Thickness), typeof(ValidatingTextBox),
                new PropertyMetadata(new Thickness(2)));

        public Thickness ErrorBorderThickness
        {
            get => (Thickness)GetValue(ErrorBorderThicknessProperty);
            set => SetValue(ErrorBorderThicknessProperty, value);
        }

        /// <summary>
        /// Mostrar tooltip con el error
        /// </summary>
        public static readonly DependencyProperty ShowErrorTooltipProperty =
            DependencyProperty.Register(nameof(ShowErrorTooltip), typeof(bool), typeof(ValidatingTextBox),
                new PropertyMetadata(true));

        public bool ShowErrorTooltip
        {
            get => (bool)GetValue(ShowErrorTooltipProperty);
            set => SetValue(ShowErrorTooltipProperty, value);
        }

        #endregion

        #region Event Handlers

        private void OnValidationError(object? sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                HasValidationError = true;
                ValidationErrorMessage = e.Error.ErrorContent?.ToString() ?? "Error de validación";
                
                if (ShowErrorTooltip)
                {
                    System.Windows.Controls.ToolTip tip = new System.Windows.Controls.ToolTip();
                    tip.Content = ValidationErrorMessage;
                    this.ToolTip = tip;
                }
            }
            else if (e.Action == ValidationErrorEventAction.Removed)
            {
                HasValidationError = false;
                ValidationErrorMessage = string.Empty;
                
                if (ShowErrorTooltip)
                {
                    this.ToolTip = null;
                }
            }
        }

        #endregion

        #region Override Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateValidationState();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            if (HasValidationError && ShowErrorTooltip)
            {
                // Mostrar tooltip al enfocar si hay error
                System.Windows.Controls.ToolTip tip = new System.Windows.Controls.ToolTip();
                tip.Content = ValidationErrorMessage;
                this.ToolTip = tip;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            // Forzar validación al perder el foco
            GetBindingExpression(TextProperty)?.UpdateSource();
        }

        #endregion

        #region Private Methods

        private void UpdateValidationState()
        {
            if (HasValidationError)
            {
                BorderBrush = ErrorBorderBrush;
                BorderThickness = ErrorBorderThickness;
            }
            else
            {
                // Restaurar valores por defecto
                ClearValue(BorderBrushProperty);
                ClearValue(BorderThicknessProperty);
            }
        }

        #endregion
    }

    /// <summary>
    /// Control ComboBox personalizado que integra validación visual
    /// </summary>
    public class ValidatingComboBox : System.Windows.Controls.ComboBox
    {
        static ValidatingComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ValidatingComboBox), 
                new FrameworkPropertyMetadata(typeof(ValidatingComboBox)));
        }

        public ValidatingComboBox()
        {
            System.Windows.Controls.Validation.AddErrorHandler(this, OnValidationError);
        }

        #region Dependency Properties

        public static readonly DependencyProperty HasValidationErrorProperty =
            DependencyProperty.Register(nameof(HasValidationError), typeof(bool), typeof(ValidatingComboBox),
                new PropertyMetadata(false));

        public bool HasValidationError
        {
            get => (bool)GetValue(HasValidationErrorProperty);
            set => SetValue(HasValidationErrorProperty, value);
        }

        public static readonly DependencyProperty ValidationErrorMessageProperty =
            DependencyProperty.Register(nameof(ValidationErrorMessage), typeof(string), typeof(ValidatingComboBox),
                new PropertyMetadata(string.Empty));

        public string ValidationErrorMessage
        {
            get => (string)GetValue(ValidationErrorMessageProperty);
            set => SetValue(ValidationErrorMessageProperty, value);
        }

        public static readonly DependencyProperty ErrorBorderBrushProperty =
            DependencyProperty.Register(nameof(ErrorBorderBrush), typeof(System.Windows.Media.Brush), typeof(ValidatingComboBox),
                new PropertyMetadata(System.Windows.Media.Brushes.Red));

        public System.Windows.Media.Brush ErrorBorderBrush
        {
            get => (System.Windows.Media.Brush)GetValue(ErrorBorderBrushProperty);
            set => SetValue(ErrorBorderBrushProperty, value);
        }

        public static readonly DependencyProperty ShowErrorTooltipProperty =
            DependencyProperty.Register(nameof(ShowErrorTooltip), typeof(bool), typeof(ValidatingComboBox),
                new PropertyMetadata(true));

        public bool ShowErrorTooltip
        {
            get => (bool)GetValue(ShowErrorTooltipProperty);
            set => SetValue(ShowErrorTooltipProperty, value);
        }

        #endregion

        #region Event Handlers

        private void OnValidationError(object? sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                HasValidationError = true;
                ValidationErrorMessage = e.Error.ErrorContent?.ToString() ?? "Error de validación";
                
                if (ShowErrorTooltip)
                {
                    System.Windows.Controls.ToolTip tip = new System.Windows.Controls.ToolTip();
                    tip.Content = ValidationErrorMessage;
                    this.ToolTip = tip;
                }
            }
            else if (e.Action == ValidationErrorEventAction.Removed)
            {
                HasValidationError = false;
                ValidationErrorMessage = string.Empty;
                
                if (ShowErrorTooltip)
                {
                    this.ToolTip = null;
                }
            }
        }

        #endregion

        #region Override Methods

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            // Forzar validación al cambiar selección
            GetBindingExpression(SelectedValueProperty)?.UpdateSource();
        }

        #endregion
    }

    /// <summary>
    /// Control para mostrar un resumen de errores de validación
    /// </summary>
    public class ValidationSummary : ItemsControl
    {
        static ValidationSummary()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ValidationSummary), 
                new FrameworkPropertyMetadata(typeof(ValidationSummary)));
        }

        #region Dependency Properties

        /// <summary>
        /// Elemento del cual mostrar errores de validación
        /// </summary>
        public static readonly DependencyProperty ValidationTargetProperty =
            DependencyProperty.Register(nameof(ValidationTarget), typeof(DependencyObject), typeof(ValidationSummary),
                new PropertyMetadata(null, OnValidationTargetChanged));

        public DependencyObject ValidationTarget
        {
            get => (DependencyObject)GetValue(ValidationTargetProperty);
            set => SetValue(ValidationTargetProperty, value);
        }

        /// <summary>
        /// Título del resumen de validación
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ValidationSummary),
                new PropertyMetadata("Errores de Validación"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Mostrar solo si hay errores
        /// </summary>
        public static readonly DependencyProperty ShowOnlyWhenErrorsProperty =
            DependencyProperty.Register(nameof(ShowOnlyWhenErrors), typeof(bool), typeof(ValidationSummary),
                new PropertyMetadata(true));

        public bool ShowOnlyWhenErrors
        {
            get => (bool)GetValue(ShowOnlyWhenErrorsProperty);
            set => SetValue(ShowOnlyWhenErrorsProperty, value);
        }

        #endregion

        #region Event Handlers

        private static void OnValidationTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValidationSummary summary)
            {
                summary.UpdateValidationSummary();
            }
        }

        #endregion

        #region Methods

        private void UpdateValidationSummary()
        {
            Items.Clear();

            if (ValidationTarget == null)
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            var errors = System.Windows.Controls.Validation.GetErrors(ValidationTarget);
            
            if (errors.Count == 0)
            {
                Visibility = ShowOnlyWhenErrors ? Visibility.Collapsed : Visibility.Visible;
                return;
            }

            Visibility = Visibility.Visible;

            foreach (var error in errors)
            {
                Items.Add(error.ErrorContent?.ToString() ?? "Error de validación");
            }
        }

        public void Refresh()
        {
            UpdateValidationSummary();
        }

        #endregion
    }
}
