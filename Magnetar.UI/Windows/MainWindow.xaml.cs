using Magnetar.UI.ViewModels;
using System.Windows;

namespace Magnetar.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}