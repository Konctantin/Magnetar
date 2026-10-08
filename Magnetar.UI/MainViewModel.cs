using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Magnetar.UI;

public class MainViewModel : INotifyPropertyChanged
{
    public event Action? RequestShowWindow;
    public event Action? RequestCloseApplication;

    // Команды для привязки из XAML
    public ICommand ShowWindowCommand { get; }

    public ICommand ExecuteActionCommand { get; }

    public ICommand ExitCommand { get; }

    public MainViewModel()
    {
        // Инициализируем команды
        ShowWindowCommand = new RelayCommand(_ => RequestShowWindow?.Invoke());
        ExecuteActionCommand = new RelayCommand(_ => ExecuteCustomAction());
        ExitCommand = new RelayCommand(_ => RequestCloseApplication?.Invoke());
    }

    // Свойство, к которому будет привязана картинка в MainWindow.xaml
    public ImageSource? CurrentImage
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    private void ExecuteCustomAction()
    {
        MessageBox.Show("Действие из ViewModel успешно выполнено!", "MVVM Трей");
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
