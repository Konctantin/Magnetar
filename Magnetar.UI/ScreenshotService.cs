using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Magnetar.UI;

internal class ScreenshotService : IDisposable
{
    private readonly Action<ImageSource> _onImageGenerated;
    private Timer? _timer;
    private int _tickCount = 0;

    // Принимаем коллбек, куда отправлять готовую картинку (в нашу ViewModel)
    public ScreenshotService(Action<ImageSource> onImageGenerated)
    {
        _onImageGenerated = onImageGenerated ?? throw new ArgumentNullException(nameof(onImageGenerated));
    }

    public void Start()
    {
        // Запуск таймера: первый тик сразу, затем каждые 100 миллисекунд (10 раз в секунду)
        _timer = new Timer(TimerTick, null, 0, 100);
    }

    private void TimerTick(object? state)
    {
        _tickCount++;

        // 1. Генерируем картинку (В РЕАЛЬНОМ ПРОЕКТЕ здесь будет получение кадра с камеры / графика / файла)
        // Для примера создаем динамический RenderTargetBitmap
        Application.Current.Dispatcher.Invoke(() =>
        {
            var image = GenerateSampleBitmap(_tickCount);

            // 2. Отправляем во ViewModel
            _onImageGenerated?.Invoke(image);
        });
    }

    private ImageSource GenerateSampleBitmap(int tick)
    {
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            // Рисуем меняющийся фон (чтобы видеть обновление 10 раз в сек)
            var color = Color.FromRgb((byte)(tick * 5 % 255), 100, 150);
            drawingContext.DrawRectangle(new SolidColorBrush(color), null, new Rect(0, 0, 200, 200));

            // Рисуем текст текущего времени
            var formattedText = new FormattedText(
                DateTime.Now.ToString("HH:mm:ss.ff"),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                20,
                Brushes.White,
                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

            drawingContext.DrawText(formattedText, new Point(20, 80));
        }

        var renderTargetBitmap = new RenderTargetBitmap(200, 200, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);

        // КРИТИЧЕСКИ ВАЖНО ДЛЯ ПАМЯТИ: Замораживаем объект.
        // Без Freeze() частые обновления вызовут утечку Unmanaged памяти в WPF графическом конвейере.
        renderTargetBitmap.Freeze();

        return renderTargetBitmap;
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
