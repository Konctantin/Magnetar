using Magnetar.UI.Services;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Magnetar.UI.Models;

public class AuraIconData : INotifyPropertyChanged
{
    private readonly AauraBuffer auraBuffer;

    public string Hash { get; set; }

    public BitmapImage Icon
    {
        get;
        set { field = value; OnPropertyChanged(); }
    } = new BitmapImage();

    public string Name
    {
        get => auraBuffer.Name;
        set
        {
            auraBuffer.Name = value;
            OnPropertyChanged();
            OnNameChanged?.Invoke();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Action OnNameChanged { get; set; }

    public AuraIconData(AauraBuffer auraBuffer)
    {
        this.auraBuffer = auraBuffer;
        this.Hash = auraBuffer.Hash;
        this.Icon = ToWpfBitmap(auraBuffer.Buffer);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static BitmapImage ToWpfBitmap(byte[] data)
    {
        using (var stream = new MemoryStream(data))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}