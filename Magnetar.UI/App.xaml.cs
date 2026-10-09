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
    private Window? mainWindow;
    private TaskbarIcon? notifyIcon;
    private MainViewModel? viewModel;

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

            viewModel = ServiceProvider.GetRequiredService<MainViewModel>();

            notifyIcon = new TaskbarIcon
            {
                IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
                ToolTipText = "Magnetar",
                ContextMenu = (ContextMenu)FindResource("TrayMenu"),
                DataContext = viewModel
            };

            notifyIcon.ForceCreate();

            notifyIcon.DoubleClickCommand = viewModel.ShowWindowCommand;
            viewModel.RequestShowWindow += OnRequestShowWindow;
            viewModel.RequestCloseApplication += OnRequestCloseApplication;


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
        if (mainWindow == null)
        {
            mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Closing += (s, args) =>
            {
                args.Cancel = true;
                mainWindow.Hide();
            };
        }

        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();
    }

    private void OnRequestCloseApplication()
    {
        ServiceProvider?.GetService<MagnetarService>()?.Dispose();

        notifyIcon?.Dispose();
        Shutdown();
    }
}
