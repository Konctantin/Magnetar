using H.NotifyIcon;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Magnetar.UI;

public partial class App : Application
{
    private Window? _mainWindow;
    private TaskbarIcon? _notifyIcon;
    private MainViewModel? _viewModel;

    ScreenshotService? _imageService;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _viewModel = (MainViewModel)FindResource("MainVM");
        var trayMenu = (ContextMenu)FindResource("TrayMenu");

        _notifyIcon = new TaskbarIcon
        {
            // Указываем путь к иконке (Build Action должен быть Resource!)
            IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
            ToolTipText = "Magnetar UI (Работает в фоне)",
            ContextMenu = trayMenu,
            DataContext = _viewModel
        };
        _notifyIcon.ForceCreate();

        if (_viewModel != null)
        {
            _notifyIcon.DoubleClickCommand = _viewModel.ShowWindowCommand;

            _viewModel.RequestShowWindow += OnRequestShowWindow;
            _viewModel.RequestCloseApplication += OnRequestCloseApplication;

            _imageService = new ScreenshotService(img => _viewModel.CurrentImage = img);
            _imageService.Start();
        }
    }

    private void OnRequestShowWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow { DataContext = _viewModel };
            _mainWindow.Closing += (s, args) =>
            {
                args.Cancel = true;
                _mainWindow.Hide();
            };
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void OnRequestCloseApplication()
    {
        _notifyIcon?.Dispose();
        Shutdown();
    }
}

