using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones de la interfaz de usuario
/// </summary>
public class UISettings : INotifyPropertyChanged
{    private string _theme = "Light";
    private string _accentColor = "#3498DB";
    private string _primaryColor = "#2ECC71";
    private string _secondaryColor = "#E74C3C";
    private int _fontSize = 14;
    private string _fontFamily = "Segoe UI";
    private bool _animations = true;
    private double _windowOpacity = 1.0;
    private bool _showProgressAnimations = true;
    private bool _confirmActionDialogs = true;
    private int _maxRecentFiles = 10;
    private bool _showToolTips = true;
    private bool _compactMode = false;

    /// <summary>
    /// Tema de la aplicación (Light, Dark, Auto)
    /// </summary>
    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }    /// <summary>
    /// Color de acento en formato hexadecimal
    /// </summary>
    public string AccentColor
    {
        get => _accentColor;
        set => SetProperty(ref _accentColor, value);
    }

    /// <summary>
    /// Color primario de la interfaz en formato hexadecimal
    /// </summary>
    public string PrimaryColor
    {
        get => _primaryColor;
        set => SetProperty(ref _primaryColor, value);
    }

    /// <summary>
    /// Color secundario de la interfaz en formato hexadecimal
    /// </summary>
    public string SecondaryColor
    {
        get => _secondaryColor;
        set => SetProperty(ref _secondaryColor, value);
    }

    /// <summary>
    /// Tamaño de fuente base
    /// </summary>
    public int FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    /// <summary>
    /// Familia de fuente
    /// </summary>
    public string FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value);
    }

    /// <summary>
    /// Habilitar animaciones en la UI
    /// </summary>
    public bool Animations
    {
        get => _animations;
        set => SetProperty(ref _animations, value);
    }

    /// <summary>
    /// Opacidad de la ventana principal (0.0 - 1.0)
    /// </summary>
    public double WindowOpacity
    {
        get => _windowOpacity;
        set => SetProperty(ref _windowOpacity, Math.Max(0.1, Math.Min(1.0, value)));
    }

    /// <summary>
    /// Mostrar animaciones de progreso
    /// </summary>
    public bool ShowProgressAnimations
    {
        get => _showProgressAnimations;
        set => SetProperty(ref _showProgressAnimations, value);
    }

    /// <summary>
    /// Mostrar diálogos de confirmación para acciones importantes
    /// </summary>
    public bool ConfirmActionDialogs
    {
        get => _confirmActionDialogs;
        set => SetProperty(ref _confirmActionDialogs, value);
    }

    /// <summary>
    /// Número máximo de archivos recientes a recordar
    /// </summary>
    public int MaxRecentFiles
    {
        get => _maxRecentFiles;
        set => SetProperty(ref _maxRecentFiles, Math.Max(0, Math.Min(50, value)));
    }

    /// <summary>
    /// Mostrar tooltips informativos
    /// </summary>
    public bool ShowToolTips
    {
        get => _showToolTips;
        set => SetProperty(ref _showToolTips, value);
    }

    /// <summary>
    /// Modo compacto para interfaces más densas
    /// </summary>
    public bool CompactMode
    {
        get => _compactMode;
        set => SetProperty(ref _compactMode, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
