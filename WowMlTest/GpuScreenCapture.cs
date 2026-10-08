using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

public class GpuScreenCapture : IDisposable
{
    private readonly Device _device;
    private readonly OutputDuplication _deskDupl;
    private readonly Texture2D _stagingTexture;
    private readonly int _width;
    private readonly int _height;

    public GpuScreenCapture(int adapterIndex = 0, int outputIndex = 0)
    {
        // Инициализируем фабрику DXGI и видеокарту (Adapter)
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

                // Создаем дубликатор экрана (DXGI Desktop Duplication)
                _deskDupl = output1.DuplicateOutput(_device);
            }
        }

        // Создаем текстуру-буфер (Staging), через которую процессор сможет читать данные из GPU
        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read, // Разрешаем чтение процессором
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,       // Стандартный формат экрана Windows (BGRA)
            Width = _width,
            Height = _height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging
        };
        _stagingTexture = new Texture2D(_device, textureDesc);
    }

    /// <summary>
    /// Функция Сверхбыстрого Скриншота. Записывает пиксели экрана в готовый буфер byte[].
    /// </summary>
    /// <param name="timeoutMs">Время ожидания изменения кадра (если экран статичен)</param>
    /// <param name="outputRawBytes">Предварительно выделенный массив размером Width * Height * 4</param>
    /// <returns>True если кадр успешно захвачен, False если изменений не было (таймаут)</returns>
    public bool CaptureScreenToBuffer(int timeoutMs, byte[] outputRawBytes)
    {
        // 1. Пытаемся захватить следующий кадр из видеокарты
        var result = _deskDupl.TryAcquireNextFrame(timeoutMs, out var frameInfo, out var desktopResource);
        try
        {
            // Если таймаут (экран не изменился) или другая ошибка — просто выходим
            if (result.Failure)
                return false;

            // 2. Получаем текстуру кадра из GPU
            using var screenTexture = desktopResource.QueryInterface<Texture2D>();
            // Скоростное копирование внутри видеопамяти
            _device.ImmediateContext.CopyResource(screenTexture, _stagingTexture);
        }
        catch (SharpDXException ex)
            when (ex.ResultCode == SharpDX.DXGI.ResultCode.WaitTimeout)
        {
            return false; // Экран не менялся, вышли по таймауту
        }
        finally
        {
            // Освобождаем кадр, чтобы видеокарта могла рендерить дальше
            desktopResource.Dispose();
            _deskDupl.ReleaseFrame();
        }

        // 3. Маппим текстуру в память CPU для быстрого чтения байт
        var dataBox = _device.ImmediateContext.MapSubresource(
            _stagingTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
        try
        {
            int rowPitch = dataBox.RowPitch; // Шаг строки в памяти (может быть чуть больше чем Width * 4)
            int bytesPerPixel = 4;           // BGRA = 4 байта

            // Построчно копируем данные в наш итоговый массив без лишних аллокаций
            for (int y = 0; y < _height; y++)
            {
                nint sourceRowPtr = dataBox.DataPointer + (y * rowPitch);
                int targetOffset = y * _width * bytesPerPixel;
                Marshal.Copy(sourceRowPtr, outputRawBytes, targetOffset, _width * bytesPerPixel);
            }
        }
        finally
        {
            _device.ImmediateContext.UnmapSubresource(_stagingTexture, 0);
        }

        return true;
    }

    public void Dispose()
    {
        _stagingTexture?.Dispose();
        _deskDupl?.Dispose();
        _device?.Dispose();

        GC.SuppressFinalize(this);
    }
}