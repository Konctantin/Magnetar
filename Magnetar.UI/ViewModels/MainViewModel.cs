using Magnetar.UI.Common;
using Magnetar.UI.Services;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Magnetar.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly MagnetarService _magnetarService;

    public event Action? RequestShowWindow;
    public event Action? RequestCloseApplication;

    // Команды для привязки из XAML
    public ICommand ShowWindowCommand { get; }

    public ICommand ExecuteActionCommand { get; }

    public ICommand ExitCommand { get; }

    public WriteableBitmap? CurrentImage
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel(MagnetarService magnetarService)
    {
        // Инициализируем команды
        ShowWindowCommand = new RelayCommand(_ => RequestShowWindow?.Invoke());
        ExecuteActionCommand = new RelayCommand(_ => ExecuteCustomAction());
        ExitCommand = new RelayCommand(_ => RequestCloseApplication?.Invoke());

        _magnetarService = magnetarService;

        // Подписываемся на события фонового сервиса
        _magnetarService = magnetarService;
        _magnetarService.AreaCaptured += OnAreaCaptured;
    }

    private void OnAreaCaptured(Bitmap sharedBitmap)
    {
        // Перенаправляем выполнение в UI-поток, так как работаем с WriteableBitmap
        Application.Current.Dispatcher.Invoke(() =>
        {
            int width = sharedBitmap.Width;
            int height = sharedBitmap.Height;

            // Инициализируем буфер один раз
            if (CurrentImage == null || CurrentImage.PixelWidth != width || CurrentImage.PixelHeight != height)
            {
                CurrentImage = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null);
            }

            // Копируем пиксели из памяти в память (0 аллокаций)
            BitmapData bitmapData = sharedBitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppRgb);

            try
            {
                CurrentImage.Lock();
                CurrentImage.WritePixels(
                    new Int32Rect(0, 0, width, height),
                    bitmapData.Scan0,
                    bitmapData.Stride * height,
                    bitmapData.Stride);
            }
            finally
            {
                CurrentImage.Unlock();
                sharedBitmap.UnlockBits(bitmapData);
            }
        });
    }

    private void ExecuteCustomAction()
    {
        MessageBox.Show("Действие из ViewModel успешно выполнено!", "MVVM Трей");
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        _magnetarService.AreaCaptured -= OnAreaCaptured;
        GC.SuppressFinalize(this);
    }
}
