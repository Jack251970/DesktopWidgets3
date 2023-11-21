using Microsoft.Win32;
using DesktopWidgets3.Contracts.Services;

namespace DesktopWidgets3.Services;

public class SessionSwitchService : ISessionSwitchService
{
    private readonly ITimersService _timersService;

    public SessionSwitchService(ITimersService timersService)
    {
        _timersService = timersService;

        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
    }

    ~SessionSwitchService()
    {
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLogon:
                SessionUnlockAction();
                break;
            case SessionSwitchReason.SessionUnlock:
                SessionUnlockAction();
                break;
            case SessionSwitchReason.SessionLock:
                SessionLockAction();
                break;
            case SessionSwitchReason.SessionLogoff:
                SessionLockAction();
                break;
        }
    }

    private void SessionLockAction()
    {
        _timersService.StopAllTimers();
    }

    private void SessionUnlockAction()
    {
        _timersService.StartAllTimersAsync();
    }
}
