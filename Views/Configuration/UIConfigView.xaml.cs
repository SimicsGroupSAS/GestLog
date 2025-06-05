using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestLog.Views.Configuration;

public partial class UIConfigView : System.Windows.Controls.UserControl
{
    public UIConfigView()
    {
        InitializeComponent();
    }    private void SelectPrimaryColor_Click(object sender, RoutedEventArgs e)
    {
        var colorDialog = new System.Windows.Forms.ColorDialog();
        
        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = colorDialog.Color;
            var wpfColor = System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
            
            if (DataContext is GestLog.Models.Configuration.UISettings settings)
            {
                settings.PrimaryColor = wpfColor.ToString();
            }
        }
    }    private void SelectSecondaryColor_Click(object sender, RoutedEventArgs e)
    {
        var colorDialog = new System.Windows.Forms.ColorDialog();
        
        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = colorDialog.Color;
            var wpfColor = System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
            
            if (DataContext is GestLog.Models.Configuration.UISettings settings)
            {
                settings.SecondaryColor = wpfColor.ToString();
            }
        }
    }
}
