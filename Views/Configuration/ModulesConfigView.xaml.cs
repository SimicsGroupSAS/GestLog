using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestLog.Views.Configuration;

public partial class ModulesConfigView : System.Windows.Controls.UserControl
{
    public ModulesConfigView()
    {
        InitializeComponent();
    }
    
    private void BtnAdvancedDaaterProcessorConfig_Click(object sender, RoutedEventArgs e)
    {
        // Buscar el control padre ConfigurationView para llamar a su método de navegación
        ConfigurationView? parentConfigView = FindParentConfigView();
        parentConfigView?.LoadDaaterProcessorConfigView();
    }
    
    private ConfigurationView? FindParentConfigView()
    {
        // Buscar el control padre ConfigurationView
        DependencyObject parent = VisualTreeHelper.GetParent(this);
        
        while (parent != null && !(parent is ConfigurationView))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        
        return parent as ConfigurationView;
    }
}
