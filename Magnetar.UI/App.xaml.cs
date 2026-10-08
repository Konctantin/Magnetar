using H.NotifyIcon;
using Magnetar.UI.Services;
using Magnetar.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]

namespace Magnetar.UI;

public partial class App : Application
{
    private Window? _mainWindow;
    private TaskbarIcon? _notifyIcon;
    private MainViewModel? _viewModel;

    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            var services = new ServiceCollection();
            services.AddSingleton(provider => UI.Properties.Settings.Default);

            services.AddSingleton<ClickerService>();
            services.AddSingleton<ScreenSnapshotService>();
            services.AddSingleton<ForegroundWindowService>();

            services.AddSingleton<StateService>();
            services.AddSingleton<RotationService>();

            services.AddSingleton<MagnetarService>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();

            ServiceProvider = services.BuildServiceProvider();

            _viewModel = ServiceProvider.GetRequiredService<MainViewModel>();

            _notifyIcon = new TaskbarIcon
            {
                IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
                ToolTipText = "Magnetar UI (Работает в фоне)",
                ContextMenu = (ContextMenu)FindResource("TrayMenu"),
                DataContext = _viewModel
            };

            _notifyIcon.ForceCreate();

            _notifyIcon.DoubleClickCommand = _viewModel.ShowWindowCommand;
            _viewModel.RequestShowWindow += OnRequestShowWindow;
            _viewModel.RequestCloseApplication += OnRequestCloseApplication;


            ServiceProvider.GetRequiredService<MagnetarService>().Start();

#if DEBUG
            OnRequestShowWindow();
#endif
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"{ex.Message}{Environment.NewLine}{Environment.NewLine}{ex}",
                "Ошибка запуска программы",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }

    private void OnRequestShowWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
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
        ServiceProvider?.GetService<MagnetarService>()?.Dispose();

        _notifyIcon?.Dispose();
        Shutdown();
    }
}
