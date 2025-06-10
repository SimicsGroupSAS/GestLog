using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GestLog.Modules.GestionCartera.Models;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Services.Core.Security;
using GestLog.Services.Core.Logging;
using GestLog.Services.Configuration;
using GestLog.Models.Configuration;

namespace GestLog.Views.Tools.GestionCartera
{
    /// <summary>
    /// Ventana de configuración SMTP - Versión corregida
    /// </summary>
    public partial class SmtpConfigurationWindow : Window
    {
        private readonly IEmailService _emailService;
        private readonly ICredentialService _credentialService;
        private readonly IConfigurationService _configurationService;
        private readonly IGestLogLogger _logger;
        private SmtpSettings _currentSettings;
        private bool _isTestSuccessful = false;
        
        public SmtpSettings Settings => _currentSettings;

        public SmtpConfigurationWindow(
            IEmailService emailService, 
            ICredentialService credentialService, 
            IConfigurationService configurationService,
            IGestLogLogger logger)
        {
            try
            {
                InitializeComponent();
                
                _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
                _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
                _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                
                _currentSettings = _configurationService.Current.Smtp ?? new SmtpSettings();
                
                _logger?.LogInformation("Configuración SMTP encontrada: Server={Server}, IsConfigured={IsConfigured}", 
                    _currentSettings.Server ?? "", _currentSettings.IsConfigured);
                
                LoadConfigurationToUI();
                UpdateUI();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error inicializando SmtpConfigurationWindow");
                throw;
            }
        }

        public SmtpConfigurationWindow(
            SmtpSettings settings, 
            IEmailService emailService, 
            ICredentialService credentialService, 
            IConfigurationService configurationService,
            IGestLogLogger logger)
            : this(emailService, credentialService, configurationService, logger)
        {
            _currentSettings = settings ?? new SmtpSettings();
            LoadConfigurationToUI();
        }

        #region Event Handlers

        private void OnFieldChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.IsLoaded && _currentSettings != null)
                {
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error en OnFieldChanged: {ErrorMessage}", ex.Message);
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.IsLoaded && _currentSettings != null)
                {
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error en OnPasswordChanged: {ErrorMessage}", ex.Message);
            }
        }

        private void GmailPreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyGmailPreset();
            UpdateUI();
        }

        private void OutlookPreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyOutlookPreset();
            UpdateUI();
        }

        private void Office365Preset_Click(object sender, RoutedEventArgs e)
        {
            ApplyOffice365Preset();
            UpdateUI();
        }

        private async void TestConfiguration_Click(object sender, RoutedEventArgs e)
        {
            await TestConfigurationAsync();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await SaveConfigurationAsync();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        #endregion

        #region Private Methods

        private void ApplyGmailPreset()
        {
            var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
            var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
            var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;

            if (hostTextBox != null && portTextBox != null && sslCheckBox != null)
            {
                hostTextBox.Text = "smtp.gmail.com";
                portTextBox.Text = "587";
                sslCheckBox.IsChecked = true;
                UpdateStatus("Configuración de Gmail aplicada", Colors.Orange);
                _logger?.LogInformation("Aplicada configuración predefinida: Gmail");
            }
        }

        private void ApplyOutlookPreset()
        {
            var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
            var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
            var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;

            if (hostTextBox != null && portTextBox != null && sslCheckBox != null)
            {
                hostTextBox.Text = "smtp-mail.outlook.com";
                portTextBox.Text = "587";
                sslCheckBox.IsChecked = true;
                UpdateStatus("Configuración de Outlook aplicada", Colors.Orange);
                _logger?.LogInformation("Aplicada configuración predefinida: Outlook");
            }
        }

        private void ApplyOffice365Preset()
        {
            var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
            var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
            var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;

            if (hostTextBox != null && portTextBox != null && sslCheckBox != null)
            {
                hostTextBox.Text = "smtp.office365.com";
                portTextBox.Text = "587";
                sslCheckBox.IsChecked = true;
                UpdateStatus("Configuración de Office 365 aplicada", Colors.Orange);
                _logger?.LogInformation("Aplicada configuración predefinida: Office 365");
            }
        }

        private void LoadConfigurationToUI()
        {
            try
            {
                _logger?.LogInformation("🔍 DIAGNÓSTICO LoadConfigurationToUI - UseAuthentication: {UseAuth}, Username: '{Username}'", 
                    _currentSettings.UseAuthentication, _currentSettings.Username ?? "");

                if (_currentSettings == null || !this.IsLoaded) 
                {
                    _logger?.LogWarning("⚠️ No se cargará configuración: CurrentSettings={IsNull}, IsLoaded={IsLoaded}", 
                        _currentSettings == null, this.IsLoaded);
                    return;
                }

                var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;
                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
                var saveCredentialsCheckBox = this.FindName("SaveCredentialsCheckBox") as System.Windows.Controls.CheckBox;

                if (hostTextBox != null)
                    hostTextBox.Text = _currentSettings.Server ?? string.Empty;
                
                if (portTextBox != null)
                    portTextBox.Text = _currentSettings.Port.ToString();
                
                if (sslCheckBox != null)
                    sslCheckBox.IsChecked = _currentSettings.UseSSL;
                
                if (emailTextBox != null)
                    emailTextBox.Text = _currentSettings.Username ?? string.Empty;

                // Cargar credenciales guardadas si existen
                if (_currentSettings.UseAuthentication && !string.IsNullOrEmpty(_currentSettings.Username))
                {
                    var credentialTarget = $"GestLog_SMTP_{_currentSettings.Username}";
                    var credentials = _credentialService?.GetCredentials(credentialTarget);
                    
                    if (credentials.HasValue && !string.IsNullOrEmpty(credentials.Value.username) && !string.IsNullOrEmpty(credentials.Value.password))
                    {
                        if (saveCredentialsCheckBox != null)
                            saveCredentialsCheckBox.IsChecked = true;
                        
                        if (passwordBox != null)
                            passwordBox.Password = credentials.Value.password;
                        
                        _logger?.LogInformation("✅ Credenciales SMTP cargadas desde el almacén seguro");
                    }
                    else
                    {
                        _logger?.LogInformation("⚠️ No se encontraron credenciales guardadas para: {Username}", _currentSettings.Username);
                    }
                }
                else
                {
                    if (_currentSettings.UseAuthentication && string.IsNullOrEmpty(_currentSettings.Username))
                    {
                        _logger?.LogWarning("⚠️ No se cargarán credenciales: UseAuthentication={UseAuth}, Username vacio={IsEmpty}", 
                            _currentSettings.UseAuthentication, string.IsNullOrEmpty(_currentSettings.Username));
                    }
                }

                UpdateStatus(_currentSettings.IsConfigured ? "Configuración cargada" : "No configurado",
                           _currentSettings.IsConfigured ? Colors.Green : Colors.Red);
                
                _logger?.LogInformation("Configuración SMTP cargada en UI - Server: {Server}, Username: {Username}, Configurado: {IsConfigured}", 
                    _currentSettings.Server ?? "", _currentSettings.Username ?? "", _currentSettings.IsConfigured);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al cargar configuración a la UI");
                UpdateStatus("Error al cargar configuración", Colors.Red);
            }
        }

        private void UpdateUI()
        {
            try
            {
                var testButton = this.FindName("TestButton") as System.Windows.Controls.Button;
                var saveButton = this.FindName("SaveButton") as System.Windows.Controls.Button;

                if (testButton == null || saveButton == null)
                {
                    return;
                }

                var isValidForTest = ValidateForTest();
                var isValidForSave = ValidateForSave();
                
                testButton.IsEnabled = isValidForTest;
                saveButton.IsEnabled = isValidForSave && _isTestSuccessful;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error en UpdateUI: {ErrorMessage}", ex.Message);
            }
        }

        private bool ValidateForTest()
        {
            try
            {
                var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;

                if (hostTextBox == null || portTextBox == null || emailTextBox == null || passwordBox == null)
                {
                    return false;
                }

                return !string.IsNullOrWhiteSpace(hostTextBox.Text) &&
                       int.TryParse(portTextBox.Text, out int port) && port > 0 && port <= 65535 &&
                       !string.IsNullOrWhiteSpace(emailTextBox.Text) &&
                       !string.IsNullOrWhiteSpace(passwordBox.Password);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateForSave()
        {
            return ValidateForTest();
        }

        private async Task TestConfigurationAsync()
        {
            try
            {
                var testButton = this.FindName("TestButton") as System.Windows.Controls.Button;
                var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;
                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;

                UpdateStatus("Probando configuración...", Colors.Orange);
                if (testButton != null)
                    testButton.IsEnabled = false;

                var testConfig = new SmtpConfiguration
                {
                    SmtpServer = hostTextBox?.Text?.Trim() ?? string.Empty,
                    Port = int.TryParse(portTextBox?.Text?.Trim(), out int port) ? port : 587,
                    EnableSsl = sslCheckBox?.IsChecked ?? true,
                    Username = emailTextBox?.Text?.Trim() ?? string.Empty,
                    Password = passwordBox?.Password ?? string.Empty
                };

                await _emailService.ConfigureSmtpAsync(testConfig);
                var isValid = await _emailService.ValidateConfigurationAsync();

                if (isValid)
                {
                    _isTestSuccessful = true;
                    UpdateStatus("✅ Configuración válida", Colors.Green);
                    _logger?.LogInformation("Configuración SMTP validada exitosamente");
                }
                else
                {
                    _isTestSuccessful = false;
                    UpdateStatus("❌ Error en la configuración", Colors.Red);
                    _logger?.LogWarning("Error en validación SMTP");
                }
            }
            catch (Exception ex)
            {
                _isTestSuccessful = false;
                UpdateStatus($"❌ Error: {ex.Message}", Colors.Red);
                _logger?.LogError(ex, "Error al probar configuración SMTP");
            }
            finally
            {
                var testButton = this.FindName("TestButton") as System.Windows.Controls.Button;
                if (testButton != null)
                    testButton.IsEnabled = true;
                UpdateUI();
            }
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                if (!_isTestSuccessful)
                {
                    System.Windows.MessageBox.Show("Debe probar la configuración antes de guardarla.", 
                                  "Validación requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var hostTextBox = this.FindName("HostTextBox") as System.Windows.Controls.TextBox;
                var portTextBox = this.FindName("PortTextBox") as System.Windows.Controls.TextBox;
                var sslCheckBox = this.FindName("SslCheckBox") as System.Windows.Controls.CheckBox;
                var emailTextBox = this.FindName("EmailTextBox") as System.Windows.Controls.TextBox;
                var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
                var saveCredentialsCheckBox = this.FindName("SaveCredentialsCheckBox") as System.Windows.Controls.CheckBox;

                // Obtener email una vez
                var email = emailTextBox?.Text?.Trim() ?? string.Empty;

                // Actualizar configuración persistente
                _currentSettings.Server = hostTextBox?.Text?.Trim() ?? string.Empty;
                _currentSettings.Port = int.TryParse(portTextBox?.Text?.Trim(), out int port) ? port : 587;
                _currentSettings.UseSSL = sslCheckBox?.IsChecked ?? true;
                _currentSettings.UseAuthentication = true;
                _currentSettings.Username = email;     // Email principal
                _currentSettings.FromEmail = email;    // Mismo email (sincronizado)
                _currentSettings.IsConfigured = true;

                // Guardar credenciales si se solicita
                if (saveCredentialsCheckBox?.IsChecked == true && !string.IsNullOrEmpty(email))
                {
                    var credentialTarget = $"GestLog_SMTP_{email}";
                    var saved = _credentialService?.SaveCredentials(
                        credentialTarget,
                        email,
                        passwordBox?.Password ?? string.Empty) ?? false;

                    if (saved)
                    {
                        _logger?.LogInformation("Credenciales SMTP guardadas de forma segura");
                    }
                    else
                    {
                        _logger?.LogWarning("No se pudieron guardar las credenciales de forma segura");
                    }
                }

                // Guardar configuración
                await _configurationService.SaveAsync();

                this.DialogResult = true;
                _logger?.LogInformation("Configuración SMTP guardada exitosamente");
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al guardar configuración SMTP");
                UpdateStatus($"Error al guardar: {ex.Message}", Colors.Red);
                System.Windows.MessageBox.Show($"Error al guardar la configuración: {ex.Message}", 
                              "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message, System.Windows.Media.Color color)
        {
            try
            {
                var statusTextBlock = this.FindName("StatusTextBlock") as TextBlock;
                var statusIndicator = this.FindName("StatusIndicator") as System.Windows.Shapes.Ellipse;

                if (statusTextBlock != null)
                    statusTextBlock.Text = message;
                
                if (statusIndicator != null)
                    statusIndicator.Fill = new SolidColorBrush(color);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error actualizando status: {ErrorMessage}", ex.Message);
            }
        }

        #endregion
    }
}
