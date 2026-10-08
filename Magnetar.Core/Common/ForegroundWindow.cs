using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Magnetar.Core.Common;

public sealed class ForegroundWindow : IDisposable
{
    private const int DefaultCaptureWidth = 200;
    private const int DefaultCaptureHeight = 100;

    private const uint DwmExtendedFrameBounds = 9;

    private readonly ScreenSnapshot _snapshot = new();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(
        IntPtr hWnd,
        StringBuilder lpString,
        int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetClassName(
        IntPtr hWnd,
        StringBuilder lpClassName,
        int nMaxCount);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        uint dwAttribute,
        out Rect pvAttribute,
        int cbAttribute);

    /// <summary>
    /// Дескриптор активного окна.
    /// </summary>
    public IntPtr Handle => GetForegroundWindow();

    /// <summary>
    /// Последний созданный снимок.
    /// </summary>
    public Bitmap Image => _snapshot.Image;

    /// <summary>
    /// Пиксели последнего снимка в формате BGRA.
    /// </summary>
    public ReadOnlyMemory<byte> PixelBytes => _snapshot.PixelBytes;

    public string Title
    {
        get
        {
            var builder = new StringBuilder(512);

            int length = GetWindowText(
                Handle,
                builder,
                builder.Capacity);

            return length > 0
                ? builder.ToString(0, length).Trim()
                : string.Empty;
        }
    }

    public string ClassName
    {
        get
        {
            var builder = new StringBuilder(512);

            int length = GetClassName(
                Handle,
                builder,
                builder.Capacity);

            return length > 0
                ? builder.ToString(0, length)
                : "<none>";
        }
    }

    public bool IsTitle(string title)
    {
        if (title == null)
            throw new ArgumentNullException(nameof(title));

        return Title.StartsWith(
            title,
            StringComparison.CurrentCultureIgnoreCase);
    }

    public bool IsTitle(params string[] titles)
    {
        if (titles == null)
            throw new ArgumentNullException(nameof(titles));

        return titles.Any(IsTitle);
    }

    public Rect GetForegroundRect()
    {
        IntPtr windowHandle = Handle;

        if (windowHandle == IntPtr.Zero)
            throw new InvalidOperationException("Активное окно не найдено.");

        int result = DwmGetWindowAttribute(
            windowHandle,
            DwmExtendedFrameBounds,
            out Rect rect,
            Marshal.SizeOf<Rect>());

        if (result != 0)
            throw new InvalidOperationException(
                $"DwmGetWindowAttribute завершился с кодом 0x{result:X8}.");

        return rect;
    }

    /// <summary>
    /// Делает снимок заданной области экрана.
    /// </summary>
    public Bitmap CaptureScreenArea(
        int left,
        int top,
        int width,
        int height)
    {
        return _snapshot.Capture(left, top, width, height);
    }

    /// <summary>
    /// Делает снимок нижней части активного окна.
    /// </summary>
    public Bitmap CaptureBottomArea(
        int width = DefaultCaptureWidth,
        int height = DefaultCaptureHeight)
    {
        Rect rect = GetForegroundRect();

        int left = rect.Left;
        int top = rect.Bottom - height;

        return _snapshot.Capture(
            left,
            top,
            width,
            height);
    }

    /// <summary>
    /// Получает цвет пикселя последнего снимка.
    /// Результат имеет формат 0xRRGGBB.
    /// </summary>
    public int GetPixelColor(int x, int y)
    {
        return _snapshot.GetPixelColor(x, y);
    }

    /// <summary>
    /// Получает пиксель последнего снимка в виде System.Drawing.Color.
    /// </summary>
    public Color GetPixel(int x, int y)
    {
        return _snapshot.GetPixel(x, y);
    }

    /// <summary>
    /// Вырезает область из последнего снимка.
    /// Полученный Bitmap необходимо освободить самостоятельно.
    /// </summary>
    public Bitmap Crop(
        int x,
        int y,
        int width,
        int height)
    {
        return _snapshot.Crop(x, y, width, height);
    }

    public void Dispose()
    {
        _snapshot.Dispose();
    }
}
