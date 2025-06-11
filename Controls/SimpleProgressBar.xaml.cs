using System.Windows;
using System.Windows.Controls;

namespace GestLog.Controls
{
    /// <summary>
    /// Control de barra de progreso simple y reutilizable
    /// </summary>
    public partial class SimpleProgressBar : System.Windows.Controls.UserControl
    {
        public SimpleProgressBar()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>
        /// Valor del progreso (0-100)
        /// </summary>
        public static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register("ProgressValue", typeof(double), typeof(SimpleProgressBar),
                new PropertyMetadata(0.0));

        public double ProgressValue
        {
            get { return (double)GetValue(ProgressValueProperty); }
            set { SetValue(ProgressValueProperty, value); }
        }

        /// <summary>
        /// Título de la barra de progreso
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SimpleProgressBar),
                new PropertyMetadata("Progreso"));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Mensaje de estado
        /// </summary>
        public static readonly DependencyProperty StatusMessageProperty =
            DependencyProperty.Register("StatusMessage", typeof(string), typeof(SimpleProgressBar),
                new PropertyMetadata(string.Empty));

        public string StatusMessage
        {
            get { return (string)GetValue(StatusMessageProperty); }
            set { SetValue(StatusMessageProperty, value); }
        }

        /// <summary>
        /// Mostrar encabezado con título
        /// </summary>
        public static readonly DependencyProperty ShowHeaderProperty =
            DependencyProperty.Register("ShowHeader", typeof(bool), typeof(SimpleProgressBar),
                new PropertyMetadata(true));

        public bool ShowHeader
        {
            get { return (bool)GetValue(ShowHeaderProperty); }
            set { SetValue(ShowHeaderProperty, value); }
        }

        /// <summary>
        /// Mostrar porcentaje
        /// </summary>
        public static readonly DependencyProperty ShowPercentageProperty =
            DependencyProperty.Register("ShowPercentage", typeof(bool), typeof(SimpleProgressBar),
                new PropertyMetadata(true));

        public bool ShowPercentage
        {
            get { return (bool)GetValue(ShowPercentageProperty); }
            set { SetValue(ShowPercentageProperty, value); }
        }

        /// <summary>
        /// Mostrar mensaje de estado
        /// </summary>
        public static readonly DependencyProperty ShowMessageProperty =
            DependencyProperty.Register("ShowMessage", typeof(bool), typeof(SimpleProgressBar),
                new PropertyMetadata(true));

        public bool ShowMessage
        {
            get { return (bool)GetValue(ShowMessageProperty); }
            set { SetValue(ShowMessageProperty, value); }
        }

        /// <summary>
        /// Altura de la barra de progreso
        /// </summary>
        public static readonly DependencyProperty BarHeightProperty =
            DependencyProperty.Register("BarHeight", typeof(double), typeof(SimpleProgressBar),
                new PropertyMetadata(12.0));

        public double BarHeight
        {
            get { return (double)GetValue(BarHeightProperty); }
            set { SetValue(BarHeightProperty, value); }
        }

        /// <summary>
        /// Color de fondo de la barra
        /// </summary>
        public static readonly DependencyProperty BarBackgroundProperty =
            DependencyProperty.Register("BarBackground", typeof(System.Windows.Media.Brush), typeof(SimpleProgressBar),
                new PropertyMetadata(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224))));

        public System.Windows.Media.Brush BarBackground
        {
            get { return (System.Windows.Media.Brush)GetValue(BarBackgroundProperty); }
            set { SetValue(BarBackgroundProperty, value); }
        }

        /// <summary>
        /// Color de primer plano de la barra
        /// </summary>
        public static readonly DependencyProperty BarForegroundProperty =
            DependencyProperty.Register("BarForeground", typeof(System.Windows.Media.Brush), typeof(SimpleProgressBar),
                new PropertyMetadata(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96))));

        public System.Windows.Media.Brush BarForeground
        {
            get { return (System.Windows.Media.Brush)GetValue(BarForegroundProperty); }
            set { SetValue(BarForegroundProperty, value); }
        }

        /// <summary>
        /// Color de fondo del control
        /// </summary>
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(System.Windows.Media.Brush), typeof(SimpleProgressBar),
                new PropertyMetadata(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245))));

        public System.Windows.Media.Brush BackgroundColor
        {
            get { return (System.Windows.Media.Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        /// <summary>
        /// Radio de las esquinas del control
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(SimpleProgressBar),
                new PropertyMetadata(new CornerRadius(5)));

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Color del título
        /// </summary>
        public static readonly DependencyProperty TitleColorProperty =
            DependencyProperty.Register("TitleColor", typeof(System.Windows.Media.Brush), typeof(SimpleProgressBar),
                new PropertyMetadata(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80))));

        public System.Windows.Media.Brush TitleColor
        {
            get { return (System.Windows.Media.Brush)GetValue(TitleColorProperty); }
            set { SetValue(TitleColorProperty, value); }
        }

        /// <summary>
        /// Color del porcentaje
        /// </summary>
        public static readonly DependencyProperty PercentageColorProperty =
            DependencyProperty.Register("PercentageColor", typeof(System.Windows.Media.Brush), typeof(SimpleProgressBar),
                new PropertyMetadata(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96))));

        public System.Windows.Media.Brush PercentageColor
        {
            get { return (System.Windows.Media.Brush)GetValue(PercentageColorProperty); }
            set { SetValue(PercentageColorProperty, value); }
        }

        /// <summary>
        /// Color del mensaje
        /// </summary>
        public static readonly DependencyProperty MessageColorProperty =
            DependencyProperty.Register("MessageColor", typeof(System.Windows.Media.Brush), typeof(SimpleProgressBar),
                new PropertyMetadata(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(127, 140, 141))));

        public System.Windows.Media.Brush MessageColor
        {
            get { return (System.Windows.Media.Brush)GetValue(MessageColorProperty); }
            set { SetValue(MessageColorProperty, value); }
        }

        /// <summary>
        /// Tamaño de fuente del mensaje
        /// </summary>
        public static readonly DependencyProperty MessageFontSizeProperty =
            DependencyProperty.Register("MessageFontSize", typeof(double), typeof(SimpleProgressBar),
                new PropertyMetadata(12.0));

        public double MessageFontSize
        {
            get { return (double)GetValue(MessageFontSizeProperty); }
            set { SetValue(MessageFontSizeProperty, value); }
        }

        #endregion
    }
}
