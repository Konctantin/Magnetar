class ScreenAnalyzer
{
    static void Main(string[] args)
    {
        int screenWidth = 1920;
        int screenHeight = 1080;

        // Выделяем массив под байты экрана ОДИН РАЗ (размер: W * H * 4 байта на BGRA пиксель)
        var rawFrameBuffer = new byte[screenWidth * screenHeight * 4];

        using var capturer = new GpuScreenCapture(adapterIndex: 0, outputIndex: 1);
        Console.WriteLine("Запуск высокоскоростного захвата. Нажмите Ctrl+C для выхода.");

        while (true)
        {
            // Захват кадра (займет 1-3 мс)
            bool success = capturer.CaptureScreenToBuffer(timeoutMs: 10, rawFrameBuffer);
            if (success)
            {

                Console.WriteLine("Кадр захвачен и готов к анализу!");
            }
        }
    }
}