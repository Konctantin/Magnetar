using Magnetar.Core.ScreenReader;
using OpenCvSharp;

namespace Magnetar.Core.Tests;

[TestClass]
public sealed class Test1
{
    ScreenReaderAbiblity abilityReader = new ScreenReaderAbiblity();

    [TestMethod]
    public void TestMethod1()
    {
        using var input = Cv2.ImRead("Images/druid_test.jpg", ImreadModes.Grayscale);
        using var template = Cv2.ImRead("Images/spell_nature_healingtouch.jpg", ImreadModes.Grayscale);
        //using var input = Mat.FromImageData(new byte[] { }, ImreadModes.Color);
        abilityReader.Read(input, template);
    }
}
