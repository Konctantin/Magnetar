using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

public class FrameCapturedEventArgs : EventArgs
{
    // Ссылка на буфер с пикселями (формат BGRA)
    public byte[] FrameBuffer { get; }
    public int Width { get; }
    public int Height { get; }

    public FrameCapturedEventArgs(byte[] buffer, int width, int height)
    {
        FrameBuffer = buffer;
        Width = width;
        Height = height;
    }
}

public class GpuScreenCapture : IDisposable
{
    private readonly Device _device;
    private readonly OutputDuplication _deskDupl;
    private readonly Texture2D _stagingTexture;
    private readonly int _width;
    private readonly int _height;

    // Потокобезопасность и управление циклом
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private bool _isCapturing = false;
    private byte[] _internalBuffer;

    // СОБЫТИЕ, на которое вы подпишетесь в MainWindow
    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;

    public GpuScreenCapture(int adapterIndex = 0, int outputIndex = 0)
    {
        using (var factory = new Factory1())
        using (var adapter = factory.GetAdapter1(adapterIndex))
        {
            _device = new Device(adapter, DeviceCreationFlags.BgraSupport);

            using (var output = adapter.GetOutput(outputIndex))
            using (var output1 = output.QueryInterface<Output1>())
            {
                var bounds = output1.Description.DesktopBounds;
                _width = bounds.Right - bounds.Left;
                _height = bounds.Bottom - bounds.Top;
                _deskDupl = output1.DuplicateOutput(_device);
            }
        }

        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = _width,
            Height = _height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging
        };
        _stagingTexture = new Texture2D(_device, textureDesc);

        // Инициализируем внутренний буфер один раз, чтобы избежать аллокаций в цикле
        _internalBuffer = new byte[_width * _height * 4];
    }

    /// <summary>
    /// Запуск постоянного захвата экрана по таймеру
    /// </summary>
    /// <param name="intervalMs">Интервал опроса (например, 10 мс для ~100 FPS)</param>
    public void Start(int intervalMs = 10)
    {
        if (_isCapturing)
            return;

        _isCapturing = true;
        _cts = new CancellationTokenSource();

        // Запускаем фоновый поток (LongRunning указывает .NET выделить под него честный поток ОС)
        Task.Factory.StartNew(() => CaptureLoop(_cts.Token, intervalMs),
            _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <summary>
    /// Остановка захвата
    /// </summary>
    public void Stop()
    {
        if (!_isCapturing) return;
        _cts?.Cancel();
        _isCapturing = false;
    }

    private void CaptureLoop(CancellationToken token, int intervalMs)
    {
        int bytesPerPixel = 4;

        while (!token.IsCancellationRequested)
        {
            var startTime = DateTime.UtcNow;
            var result = _deskDupl.TryAcquireNextFrame(5, out var frameInfo, out var desktopResource);
            try
            {
                // Пытаемся взять кадр. Таймаут ставим минимальный, чтобы не блокировать поток

                if (result.Success && desktopResource != null)
                {
                    using (var screenTexture = desktopResource.QueryInterface<Texture2D>())
                    {
                        _device.ImmediateContext.CopyResource(screenTexture, _stagingTexture);
                    }

                    // Освобождаем ресурсы DXGI сразу после копирования
                    desktopResource.Dispose();
                    desktopResource = null;
                    _deskDupl.ReleaseFrame();

                    // Читаем данные из Staging текстуры в CPU память
                    var dataBox = _device.ImmediateContext.MapSubresource(
                        _stagingTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                    try
                    {
                        int rowPitch = dataBox.RowPitch;
                        for (int y = 0; y < _height; y++)
                        {
                            IntPtr sourceRowPtr = dataBox.DataPointer + (y * rowPitch);
                            int targetOffset = y * _width * bytesPerPixel;
                            Marshal.Copy(sourceRowPtr, _internalBuffer, targetOffset, _width * bytesPerPixel);
                        }
                    }
                    finally
                    {
                        // Если токен отмены уже сработал, значит устройство могло быть уничтожено,
                        // и вызывать UnmapSubresource небезопасно
                        if (!token.IsCancellationRequested)
                        {
                            _device.ImmediateContext.UnmapSubresource(_stagingTexture, 0);
                        }
                    }

                    // Генерируем событие и передаем буфер подписчикам
                    // Передается ссылка на _internalBuffer (0 байт аллокации!)
                    FrameCaptured?.Invoke(this, new FrameCapturedEventArgs(_internalBuffer, _width, _height));
                }
            }
            catch (SharpDXException ex) when (ex.ResultCode == SharpDX.DXGI.ResultCode.WaitTimeout)
            {
                // Экран не изменился — DXGI не выдал кадр. Это нормально.
                // В этом случае мы можем либо пропустить шаг, либо (если вам СТРОГО нужно событие каждые 10мс
                // даже для статичной картинки) вызывать событие с прошлым состоянием буфера:
                FrameCaptured?.Invoke(this, new FrameCapturedEventArgs(_internalBuffer, _width, _height));
            }
            catch (Exception)
            {
                // На случай критических сбоев (например, при смене разрешения экрана)
                desktopResource?.Dispose();
            }

            // Вычисляем точное время сна, учитывая сколько ушло на обработку кадра (минимизирует джиттер)
            var elapsed = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            int sleepTime = intervalMs - elapsed;

            if (sleepTime > 0)
            {
                Thread.Sleep(sleepTime);
            }
        }
    }

    public void Dispose()
    {
        // 1. Посылаем сигнал отмены потоку
        Stop();

        // 2. Даем фоновому циклу небольшую паузу (50-100 мс),
        // чтобы он успел выйти из блока try-finally и завершить итерацию
        Thread.Sleep(100);

        // 3. Только ТЕПЕРЬ безопасно уничтожаем ресурсы DirectX
        _stagingTexture?.Dispose();
        _deskDupl?.Dispose();
        _device?.Dispose();
        _cts?.Dispose();

        GC.SuppressFinalize(this);
    }
}
