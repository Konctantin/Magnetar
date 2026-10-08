using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace WowMl.UI;

public class YoloInference : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;

    // Задайте размер, под который обучалась ваша модель (у YOLO это обычно 640)
    private const int ModelInputSize = 640;

    public YoloInference(string modelPath)
    {
        // Настраиваем запуск СТРОГО на RTX 4080 через CUDA
        var sessionOptions = new SessionOptions();
        var cudaOptions = new OrtCUDAProviderOptions();
        sessionOptions.AppendExecutionProvider_CUDA(cudaOptions); // Привязка к GPU

        _session = new InferenceSession(modelPath, sessionOptions);
        _inputName = _session.InputMetadata.Keys.First();
    }

    /// <summary>
    /// Главный метод подготовки кадра и отправки в нейросеть
    /// </summary>
    public unsafe void ProcessAndAnalyze(byte[] bgraRawBytes, int width, int height)
    {
        // Фиксируем массив байт в памяти, чтобы получить прямой указатель для OpenCV
        fixed (byte* pBytes = bgraRawBytes)
        {
            // 1. Оборачиваем сырые байты в матрицу OpenCV через указатель IntPtr
            using Mat rawFrame = Mat.FromPixelData(height, width, MatType.CV_8UC4, (nint)pBytes);
            using Mat bgrFrame = new();
            using Mat resizedFrame = new();
            // Избавляемся от Альфа-канала (BGRA -> BGR)
            Cv2.CvtColor(rawFrame, bgrFrame, ColorConversionCodes.BGRA2BGR);

            // Сжимаем до размеров входа нейросети (640x640)
            Cv2.Resize(bgrFrame, resizedFrame, new Size(ModelInputSize, ModelInputSize));

            // 2. Трансформируем картинку в массив float (CHW) с нормализацией
            var processedData = ExtractAndNormalizeCHW(resizedFrame);

            // 3. Создаем тензор для OnnxRuntime
            var inputTensor = new DenseTensor<float>(processedData, [1, 3, ModelInputSize, ModelInputSize]);

            var inputs = new List<NamedOnnxValue> {
                NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
            };

            // 4. Запуск инференса на RTX 4080 через CUDA
            using var outputs = _session.Run(inputs);
            var outputTensor = outputs.First().AsTensor<float>();
            ParseYoloOutput(outputTensor);
        }
    }

    /// <summary>
    /// Сверхбыстрый перевод матрицы OpenCV в формат CHW Float с нормализацией (0.0 - 1.0)
    /// </summary>
    private float[] ExtractAndNormalizeCHW(Mat image)
    {
        int channelSize = ModelInputSize * ModelInputSize;
        float[] chwArray = new float[3 * channelSize];

        // Используем небезопасный код (unsafe) для прямого чтения указателей памяти OpenCV.
        // Это в 10 раз быстрее, чем вызов метода image.At<Vec3b>() в циклах.
        unsafe
        {
            byte* scan0 = (byte*)image.Data.ToPointer();
            int stride = (int)image.Step();

            for (int y = 0; y < ModelInputSize; y++)
            {
                byte* row = scan0 + (y * stride);
                for (int x = 0; x < ModelInputSize; x++)
                {
                    // В OpenCV формат BGR, а YOLO ожидает RGB
                    byte b = row[x * 3 + 0];
                    byte g = row[x * 3 + 1];
                    byte r = row[x * 3 + 2];

                    int pixelIndex = y * ModelInputSize + x;

                    // Записываем каналы раздельно (Формат CHW) и делим на 255.0f
                    chwArray[pixelIndex] = r / 255.0f;               // Слой R
                    chwArray[channelSize + pixelIndex] = g / 255.0f; // Слой G
                    chwArray[2 * channelSize + pixelIndex] = b / 255.0f; // Слой B
                }
            }
        }

        return chwArray;
    }

    private void ParseYoloOutput(Tensor<float> outputTensor)
    {
        // Здесь будет ваш парсинг геометрии рамок (Bounding Boxes)
        // Массив содержит координаты X, Y, W, H и вероятности для каждого класса.
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}