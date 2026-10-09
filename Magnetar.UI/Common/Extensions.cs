using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Magnetar.UI.Common;

public static class Extensions
{
    public static BitmapImage? GetIconFromEnum(Enum value)
    {
        var image = new BitmapImage(new Uri($"pack://application:,,,/images/{value.ToString().ToLower()}.png"));
        return image;
    }
}
