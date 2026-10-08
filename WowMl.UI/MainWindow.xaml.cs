using Magnetar.Core.ScreenReader;
using OpenCvSharp;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WowMl.UI;

public partial class MainWindow : System.Windows.Window
{
    //private YoloInference _yolo;

    private GpuScreenCapture _capturer;
    private WriteableBitmap writableBitmap;
    private Int32Rect _rect;
    private int _stride;

    private Mat _templateMat;

    ScreenReaderAbiblity areader = new ScreenReaderAbiblity();

    // Атомарный флаг: 0 = свободен, 1 = занят обработкой кадра
    private int _isProcessingFrame = 0;

    public MainWindow()
    {
        //_yolo = new YoloInference("best_model.onnx");
        _capturer = new GpuScreenCapture(adapterIndex: 0, outputIndex: 1);
        const int screenWidth = 1920;
        const int screenHeight = 1080;
        _rect = new Int32Rect(0, 0, screenWidth, screenHeight);
        _stride = screenWidth * 4;

        writableBitmap = new WriteableBitmap(screenWidth, screenHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
        _templateMat = Cv2.ImRead("spell_nature_healingtouch.jpg", ImreadModes.Grayscale);

        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UI_RenderImage.Source = writableBitmap;
        _capturer.FrameCaptured += OnFrameCaptured;
        _capturer.Start(intervalMs: 50);
    }

    // Этот метод вызывается в фоновом потоке каждые 10 мс
    private void OnFrameCaptured(object? sender, FrameCapturedEventArgs e)
    {
        if (Interlocked.CompareExchange(ref _isProcessingFrame, 1, 0) == 1)
        {
            return;
        }

        var buffer = e.FrameBuffer;

        try
        {
            // Создаем временную матрицу, которая смотрит прямо на массив байт e.FrameBuffer
            // Замените _rect.Width и _rect.Height на актуальные размеры кадра (например 1920x1080)
            int width = _rect.Width;
            int height = _rect.Height;

            // Матрица BGRA (4 канала, CV_8UC4)
            using (var frameMat = new Mat(height, width, MatType.CV_8UC4))
            {
                // Загружаем массив байт в неуправляемую память OpenCV
                Marshal.Copy(buffer, 0, frameMat.Data, buffer.Length);

                // Ищем шаблон и рисуем рамку прямо в frameMat
                var foundResult = areader.Read(frameMat, _templateMat);

                if (foundResult != null)
                {
                    // Тут можно сохранить координаты foundResult.Value в список для кликов
                }

                // Выгружаем измененные пиксели (с нарисованной рамкой) обратно в e.FrameBuffer
                Marshal.Copy(frameMat.Data, buffer, 0, buffer.Length);
            }
        }
        catch (Exception ex)
        {
            // Логирование ошибок OpenCV, чтобы поток не падал
            System.Diagnostics.Debug.WriteLine($"OpenCV Error: {ex.Message}");
        }

        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
            try
            {
                writableBitmap.Lock();
                writableBitmap.WritePixels(_rect, buffer, _stride, 0);
                writableBitmap.AddDirtyRect(_rect);
                writableBitmap.Unlock();

                UI_RenderImage.InvalidateVisual();
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessingFrame, 0);
            }

        }), System.Windows.Threading.DispatcherPriority.Render);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_capturer != null)
        {
            _capturer.FrameCaptured -= OnFrameCaptured;
            _capturer.Dispose();
        }
        base.OnClosed(e);
    }
}
