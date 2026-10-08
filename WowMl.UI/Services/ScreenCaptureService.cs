using Magnetar.Core.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WowMl.UI.Services;

public sealed class ScreenCaptureService : IDisposable
{
    private readonly ForegroundWindow _foregroundWindow;
    private readonly Dispatcher _dispatcher;

    private readonly int _width;
    private readonly int _height;
    private readonly TimeSpan _interval;

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _captureTask;

    public event Action<BitmapSource>? FrameReady;

    public ScreenCaptureService(
        Dispatcher dispatcher,
        int width = 200,
        int height = 100,
        int framesPerSecond = 10)
    {
        if (framesPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond));

        _dispatcher = dispatcher;
        _foregroundWindow = new ForegroundWindow();

        _width = width;
        _height = height;
        _interval = TimeSpan.FromSeconds(1.0 / framesPerSecond);
    }

    public void Start()
    {
        if (_captureTask != null)
            return;

        _cancellationTokenSource = new CancellationTokenSource();

        _captureTask = CaptureLoopAsync(
            _cancellationTokenSource.Token);
    }

    private async Task CaptureLoopAsync(
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                BitmapSource? frame = null;

                try
                {
                    frame = _foregroundWindow
                        .CaptureBottomBitmapSource(
                            _width,
                            _height);
                }
                catch
                {
                    // Здесь можно добавить логирование.
                    // Например, активное окно могло исчезнуть
                    // в момент захвата.
                    continue;
                }

                // Передаём готовый BitmapSource в UI-поток.
                // Сам BitmapSource уже заморожен через Freeze().
                await _dispatcher.InvokeAsync(
                    () => FrameReady?.Invoke(frame),
                    DispatcherPriority.Render);
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение.
        }
    }

    public async Task StopAsync()
    {
        if (_captureTask == null)
            return;

        _cancellationTokenSource?.Cancel();

        try
        {
            await _captureTask;
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение.
        }

        _captureTask = null;

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _foregroundWindow.Dispose();
    }
}