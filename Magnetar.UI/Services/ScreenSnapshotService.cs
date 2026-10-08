using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Size = System.Drawing.Size;

namespace Magnetar.UI.Services;

[StructLayout(LayoutKind.Sequential)]
public struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;

    public readonly int Height => Bottom - Top;

    public override readonly string ToString()
        => $"Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}";

    public static readonly int Size = Marshal.SizeOf<Rect>();
}

/// <summary>
/// Переиспользуемый снимок области экрана.
/// </summary>
public sealed class ScreenSnapshotService : IDisposable
{
    private Bitmap? _image;
    private byte[] _pixelBytes = [];

    private WriteableBitmap? _bitmapSource;

    /// <summary>
    /// Основное изображение System.Drawing.
    /// Этот объект переиспользуется между захватами,
    /// если размеры области не изменились.
    /// </summary>
    public Bitmap Image =>
        _image ?? throw new InvalidOperationException(
            "Снимок ещё не создан.");

    public int Width => _image?.Width ?? 0;

    public int Height => _image?.Height ?? 0;

    /// <summary>
    /// Пиксели последнего снимка в формате BGRA.
    /// На каждый пиксель приходится 4 байта:
    /// B, G, R, A.
    /// </summary>
    public ReadOnlyMemory<byte> PixelBytes => _pixelBytes;

    /// <summary>
    /// Переиспользуемый WPF-источник изображения.
    /// Объект создаётся один раз и затем обновляется через WritePixels.
    /// </summary>
    public WriteableBitmap BitmapSource =>
        _bitmapSource ?? throw new InvalidOperationException(
            "BitmapSource ещё не создан.");

    /// <summary>
    /// Захватывает область экрана.
    /// Метод можно вызывать из фонового потока.
    /// </summary>
    public Bitmap Capture(int left, int top, int width, int height)
    {
        ValidateSize(width, height);
        EnsureImage(width, height);
        using (Graphics graphics = Graphics.FromImage(Image))
        {
            graphics.CopyFromScreen(
                sourceX: left,
                sourceY: top,
                destinationX: 0,
                destinationY: 0,
                blockRegionSize: new Size(width, height),
                copyPixelOperation: CopyPixelOperation.SourceCopy);
        }

        UpdatePixelBuffer();
        return Image;
    }

    /// <summary>
    /// Создаёт переиспользуемый WriteableBitmap.
    /// Метод должен вызываться из UI-потока.
    /// </summary>
    public WriteableBitmap InitializeBitmapSource(int width, int height)
    {
        ValidateSize(width, height);
        EnsureImage(width, height);

        if (_bitmapSource == null ||
            _bitmapSource.PixelWidth != width ||
            _bitmapSource.PixelHeight != height)
        {
            _bitmapSource = new WriteableBitmap(
                pixelWidth: width,
                pixelHeight: height,
                dpiX: 96,
                dpiY: 96,
                pixelFormat: PixelFormats.Bgra32,
                palette: null);
        }

        return _bitmapSource;
    }

    /// <summary>
    /// Копирует последние захваченные пиксели в WriteableBitmap.
    /// Метод должен вызываться из UI-потока.
    /// </summary>
    public WriteableBitmap UpdateBitmapSource()
    {
        if (_bitmapSource == null)
        {
            throw new InvalidOperationException(
                "Сначала вызовите InitializeBitmapSource.");
        }

        if (!_bitmapSource.Dispatcher.CheckAccess())
        {
            throw new InvalidOperationException(
                "UpdateBitmapSource необходимо вызывать из UI-потока.");
        }

        _bitmapSource.WritePixels(
            sourceRect: new Int32Rect(0, 0, Width, Height),
            pixels: _pixelBytes,
            stride: Width * 4,
            offset: 0);

        return _bitmapSource;
    }

    /// <summary>
    /// Возвращает цвет пикселя в формате 0xRRGGBB.
    /// Альфа-канал не используется.
    /// </summary>
    public int GetPixelColor(int x, int y)
    {
        ValidateCoordinates(x, y);

        int offset = (y * Width + x) * 4;

        byte blue  = _pixelBytes[offset];
        byte green = _pixelBytes[offset + 1];
        byte red   = _pixelBytes[offset + 2];

        return (red << 16) | (green << 8) | blue;
    }

    /// <summary>
    /// Возвращает пиксель в виде System.Drawing.Color.
    /// </summary>
    public Color GetPixel(int x, int y)
    {
        ValidateCoordinates(x, y);

        int offset = (y * Width + x) * 4;

        byte blue  = _pixelBytes[offset];
        byte green = _pixelBytes[offset + 1];
        byte red   = _pixelBytes[offset + 2];

        return Color.FromArgb(red, green, blue);
    }

    /// <summary>
    /// Возвращает копию части уже захваченного изображения.
    /// Полученный Bitmap необходимо освободить вызывающему коду.
    /// </summary>
    public Bitmap Crop(int x, int y, int width, int height)
    {
        if (_image == null)
        {
            throw new InvalidOperationException(
                "Сначала необходимо создать снимок.");
        }

        ValidateSize(width, height);

        if (x < 0 || y < 0 || x + width > Width || y + height > Height)
        {
            throw new ArgumentException(
                "Область обрезки должна полностью находиться внутри снимка.");
        }

        return _image.Clone(
            new Rectangle(x, y, width, height),
            PixelFormat.Format32bppArgb);
    }

    private void EnsureImage(int width, int height)
    {
        if (_image != null && _image.Width == width && _image.Height == height)
        {
            return;
        }

        _image?.Dispose();

        _image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        _pixelBytes = new byte[checked(width * height * 4)];

        // Если размеры изменились, старый WriteableBitmap больше
        // не соответствует массиву пикселей.
        _bitmapSource = null;
    }

    private void UpdatePixelBuffer()
    {
        if (_image == null)
            return;

        Rectangle rectangle = new(0, 0, _image.Width, _image.Height);
        BitmapData bitmapData = _image.LockBits(rectangle,
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            int rowSize = Width * 4;
            if (bitmapData.Stride == rowSize)
            {
                Marshal.Copy(bitmapData.Scan0, _pixelBytes, 0, _pixelBytes.Length);
                return;
            }

            // Обработка Bitmap с нестандартным stride.
            for (int y = 0; y < Height; y++)
            {
                var sourceRow = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                Marshal.Copy(sourceRow, _pixelBytes, y * rowSize, rowSize);
            }
        }
        finally
        {
            _image.UnlockBits(bitmapData);
        }
    }

    private void ValidateCoordinates(int x, int y)
    {
        if (_image == null)
        {
            throw new InvalidOperationException(
                "Сначала необходимо создать снимок.");
        }

        if (x < 0 || x >= Width)
            throw new ArgumentOutOfRangeException(nameof(x));

        if (y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException(nameof(y));
    }

    private static void ValidateSize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;

        _pixelBytes = [];
        _bitmapSource = null;
    }
}