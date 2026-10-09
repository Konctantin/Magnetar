using ICSharpCode.AvalonEdit.Document;
using Magnetar.UI.Common;
using System.Windows.Media;

namespace Magnetar.UI.Models;

public class WowClassEntity
{
    public WowClass Class { get; set; }

    public ImageSource? ClassImage {
        get
        {
            var i = Extensions.GetIconFromEnum(Class);
            return i;
        }
    }

    public TextDocument Code { get; set; } = new TextDocument();
}
