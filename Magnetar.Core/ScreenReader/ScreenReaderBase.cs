using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Magnetar.Core.ScreenReader;

public abstract class ScreenReaderBase
{
    public abstract Rect? Read(Mat input, Mat template);
}
