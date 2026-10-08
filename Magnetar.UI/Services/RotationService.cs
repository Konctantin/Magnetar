namespace Magnetar.UI.Services;

public class RotationService(
    ForegroundWindowService foregroundService,
    StateService state,
    ClickerService clicker)
{
    public void Process()
    {
        if (!state.IsValidArea)
            return;

        if (!state.IsInWorld)
            return;

        // todo: process rotation sequence

        var key = new Common.KeyRecord(0, 0);
        clicker.Click(foregroundService.Handle, key);
    }
}
