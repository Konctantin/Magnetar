using OpenCvSharp;

namespace WowMl.UI;

public class IconScaner
{
    public unsafe static void Scan(byte[] rawBytes, int width, int height)
    {
        var matType = MatType.CV_8UC4;
        int step = width * matType.Channels * matType.Depth;

        using var templateFull = new Mat();
        using var templateSmall = new Mat();

        fixed (byte* b = rawBytes)
        {
            var size = new Size(width, height);
            // 1. Создаем матрицу из ваших сырых байт Full HD
            using var screenFull = Mat.FromPixelData(width, height, matType, (nint)b, step);
            using var grayFull = new Mat();
            using var graySmall = new Mat();

            // Переводим в черно-белый формат
            Cv2.CvtColor(screenFull, grayFull, ColorConversionCodes.BGRA2GRAY);

            // Уменьшаем разрешение ровно в 2 раза (из 1920x1080 получаем 960x540)
            Cv2.PyrDown(grayFull, graySmall);

            // 2. Ищем уменьшенный шаблон на уменьшенном экране
            // (Ваш шаблон 'templateSmall' тоже должен быть заранее уменьшен в 2 раза!)
            using var result = new Mat();
            Cv2.MatchTemplate(graySmall, templateSmall, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out var maxLoc);

            if (maxVal > 0.75)
            {
                // !!! ВАЖНО !!!
                // Найденные координаты (maxLoc) соответствуют маленькому экрану.
                // Чтобы узнать, где кнопка находится на настоящем Full HD экране,
                // просто умножаем координаты на 2:
                int realX = maxLoc.X * 2;
                int realY = maxLoc.Y * 2;
                int realWidth = templateFull.Width;   // Оригинальная ширина кнопки
                int realHeight = templateFull.Height; // Оригинальная высота кнопки

                var fullSizeButtonRect = new Rect(realX, realY, realWidth, realHeight);

                // Сохраняем эти Full HD координаты в ваш список кнопок!
            }
        }
    }
}
