using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Magnetar.UI.Models;

public enum AuraType
{
    None, Player, Target, Focus
}

public class AuraData : IDisposable
{
    public const int ICON_SIZE = 10;

    public bool Exists { get; set; }

    public AuraType Type { get; set; } = AuraType.None;

    public int Count { get; set; }

    public int Remains { get; set; }

    public string Name { get; set; } = "";

    public string Hash { get; set; } = "";

    public Bitmap Image { get; set; } = new Bitmap(ICON_SIZE, ICON_SIZE);

    public Graphics ImageGraphics { get; init; }

    private readonly MemoryStream _memoryStream = new(1024);

    public AuraData()
    {
        ImageGraphics = Graphics.FromImage(Image);
    }

    public void CropIcon(Bitmap source, int x, int y)
    {
        lock (source)
        {
            ImageGraphics.DrawImage(source,
                new Rectangle(0, 0, ICON_SIZE, ICON_SIZE),
                new Rectangle(x, y, ICON_SIZE, ICON_SIZE),
                GraphicsUnit.Pixel);

            Image.Save(_memoryStream, ImageFormat.Bmp);
            Hash = NoHash(Image);
        }
    }

    public byte[] IconBytes => _memoryStream.GetBuffer();

    public void Dispose()
    {
        ImageGraphics.Dispose();
        Image.Dispose();
        GC.SuppressFinalize(this);
    }

    public string NoHash(Bitmap bitmap)
    {
        StringBuilder sb = new StringBuilder();
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color current = bitmap.GetPixel(x, y);
                sb.Append($"{current.R:X2}{current.G:X2}{current.B:X2}");
            }
        }

        return sb.ToString();
    }

    public static string GetColorClusterHash(Bitmap bitmap, int step = 32)
    {
        // step = 32 дает отличный баланс: отсекает шум, но сохраняет цвета.
        // Например, цвета (241, 12, 5) и (253, 15, 2) превратятся в один хэш.

        StringBuilder sb = new StringBuilder();

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color current = bitmap.GetPixel(x, y);

                // Округляем каналы до ближайшего шага
                int r = (current.R / step) * step;
                int g = (current.G / step) * step;
                int b = (current.B / step) * step;

                // Записываем округленный цвет в строку (например: "F0A000")
                sb.Append($"{r:X2}{g:X2}{b:X2}");
            }
        }

        return sb.ToString();
    }

    public static string GetCoarseColorHash(Bitmap bitmap)
    {
        StringBuilder sb = new StringBuilder();
        int step = 85;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color current = bitmap.GetPixel(x, y);

                // Округляем до 0, 85, 170 или 255
                int r = (int)(Math.Round((double)current.R / step) * step);
                int g = (int)(Math.Round((double)current.G / step) * step);
                int b = (int)(Math.Round((double)current.B / step) * step);

                sb.Append($"{r:X2}{g:X2}{b:X2}");
            }
        }
        return sb.ToString();
    }
}
