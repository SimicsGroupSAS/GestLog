using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GestLog.Views;
using System.Collections.Generic;

namespace GestLog;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Stack<(UserControl view, string title)> _navigationStack;
    private UserControl? _currentView;

    public MainWindow()
    {
        InitializeComponent();
        _navigationStack = new Stack<(UserControl, string)>();
        
        // Cargar la vista Home por defecto
        LoadHomeView();
    }

    private void LoadHomeView()
    {
        var homeView = new HomeView();
        contentPanel.Content = homeView;
        _currentView = homeView;
        txtCurrentView.Text = "Home";
        btnBack.Visibility = Visibility.Collapsed;
        _navigationStack.Clear();
    }

    public void NavigateToView(UserControl view, string title)
    {
        // Guardar la vista actual en el stack
        if (_currentView != null)
        {
            _navigationStack.Push((_currentView, txtCurrentView.Text));
        }

        // Navegar a la nueva vista
        contentPanel.Content = view;
        _currentView = view;
        txtCurrentView.Text = title;
        btnBack.Visibility = Visibility.Visible;
    }

    private void btnHome_Click(object sender, RoutedEventArgs e)
    {
        LoadHomeView();
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        if (_navigationStack.Count > 0)
        {
            var (previousView, previousTitle) = _navigationStack.Pop();
            contentPanel.Content = previousView;
            _currentView = previousView;
            txtCurrentView.Text = previousTitle;

            // Si no hay más vistas en el stack, ocultar el botón Back
            if (_navigationStack.Count == 0)
            {
                btnBack.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            // Si no hay stack, ir a Home
            LoadHomeView();
        }
    }

    // Método para compatibilidad con código existente
    public void SetContent(UserControl control)
    {
        contentPanel.Content = control;
        _currentView = control;
    }
}