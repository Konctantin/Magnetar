using Magnetar.UI.Properties;
using System.ComponentModel;
using System.Drawing;
using System.Windows;

namespace Magnetar.UI.Services;

public class MagnetarService : IDisposable
{
    private readonly ForegroundWindowService _foregroundService;
    private readonly StateService _stateService;
    private readonly RotationService _rotationService;
    private readonly Settings _settings;
    private Timer? _timer;

    public event Action<Bitmap>? AreaCaptured;

    public MagnetarService(
        ForegroundWindowService foregroundService,
        StateService stateService,
        RotationService rotationService,
        Settings settings)
    {
        _foregroundService = foregroundService;
        _rotationService = rotationService;
        _stateService = stateService;
        _settings = settings;
        _settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_settings.Interval))
        {
            UpdateTimerInterval();
        }
    }

    private void UpdateTimerInterval()
    {
        _timer?.Change(_settings.Interval, Timeout.Infinite);
    }

    private void TimerTick(object? state)
    {
        Application.Current.Dispatcher.Invoke(() => {
            try
            {
                if (_foregroundService.Handle == IntPtr.Zero)
                    return;

                var titles = _settings.WowTitles.Split([',', ';'],
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                //if (!_foregroundService.IsTitle(titles))
                //    return;

                _foregroundService.CaptureBottomArea(_settings.AreaWidth, _settings.AreaHeight);
                _stateService.Parse();
                _rotationService.Process();

                if (_foregroundService.Image is Bitmap bitmap)
                {
                    AreaCaptured?.Invoke(bitmap);
                }

                // _foregroundService.Image;
                // todo: more logic
            }
            finally
            {
                _timer?.Change(_settings.Interval, Timeout.Infinite);
            }
        });
    }

    public void Start()
    {
        _timer = new Timer(TimerTick, null, 0, Timeout.Infinite);
    }

    public void Dispose()
    {
        _settings.PropertyChanged -= OnSettingsChanged;
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
