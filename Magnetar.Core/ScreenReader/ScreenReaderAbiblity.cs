using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Magnetar.Core.ScreenReader;

public class ScreenReaderAbiblity : ScreenReaderBase
{
    public override OpenCvSharp.Rect? Read(Mat input, Mat template)
    {
        using var grayFull = new Mat();
        using var graySmall = new Mat();

        // 1. Конвертируем входной цветной кадр (BGRA) в черно-белый (Grayscale) для MatchTemplate
        Cv2.CvtColor(input, grayFull, ColorConversionCodes.BGRA2GRAY);

        // 2. Уменьшаем разрешение черно-белого кадра в 2 раза для ускорения поиска
        Cv2.PyrDown(grayFull, graySmall);

        // 3. Ищем уменьшенный шаблон
        using var result = new Mat();
        Cv2.MatchTemplate(graySmall, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out var maxLoc);

        if (maxVal > 0.5)
        {
            // Восстанавливаем координаты для Full HD экрана
            int realX = maxLoc.X * 2;
            int realY = maxLoc.Y * 2;

            // ВАЖНО: Если ваш шаблон 'template' уже уменьшен в 2 раза,
            // то размеры рамки для Full HD тоже нужно умножить на 2
            int realWidth = template.Width * 2;
            int realHeight = template.Height * 2;

            var foundRect = new OpenCvSharp.Rect(realX, realY, realWidth, realHeight);

            // 4. Рисуем красный прямоугольник прямо поверх исходного ЦВЕТНОГО кадра (input)
            // Scalar в OpenCvSharp для BGRA: B=0, G=0, R=255, A=255 (Красный цвет)
            // Толщина линии = 5 пикселей
            Cv2.Rectangle(input, foundRect, new Scalar(0, 0, 255, 255), thickness: 5);

            return foundRect; // Возвращаем координаты (пригодятся для кликов или логики)
        }

        return null; // Ничего не нашли
    }
}
