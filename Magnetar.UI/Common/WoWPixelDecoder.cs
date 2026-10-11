using System.Drawing;
using System.Text;

namespace Magnetar.UI.Common;

public static class WoWPixelDecoder
{
    /// <summary>
    /// Расшифровывает UTF-8 текст из матрицы цветов любого размера.
    /// Обход идет слева направо, сверху вниз.
    /// </summary>
    /// <param name="pixelMatrix">Двумерный массив цветов [Y, X], где Y - высота, X - ширина</param>
    public static string DecodeTextFromMatrix(Color[,] pixelMatrix)
    {
        if (pixelMatrix == null)
        {
            throw new ArgumentNullException(nameof(pixelMatrix), "Матрица не должна быть null.");
        }

        // Автоматически берем размеры из переданного массива
        int heightY = pixelMatrix.GetLength(0); // Размер по Y (строки)
        int widthX = pixelMatrix.GetLength(1);  // Размер по X (колонки)

        var rawBytes = new List<byte>();
        bool isEndReached = false;

        // Идем СВЕРХУ ВНИЗ (по строкам Y)
        for (int y = 0; y < heightY; y++)
        {
            if (isEndReached) break;

            // Идем СЛЕВА НАПРАВО (по колонкам X)
            for (int x = 0; x < widthX; x++)
            {
                var pixel = pixelMatrix[y, x];

                // Округляем float-значения цвета до байта (0-255)
                byte r = pixel.R;
                byte g = pixel.G;
                byte b = pixel.B;

                // Проверяем R канал
                if (r == 0) { isEndReached = true; break; }
                rawBytes.Add(r);

                // Проверяем G канал
                if (g == 0) { isEndReached = true; break; }
                rawBytes.Add(g);

                // Проверяем B канал
                if (b == 0) { isEndReached = true; break; }
                rawBytes.Add(b);
            }
        }

        if (rawBytes.Count == 0)
        {
            return string.Empty;
        }
        var result = Encoding.UTF8.GetString(rawBytes.ToArray());
        return result;
    }
}