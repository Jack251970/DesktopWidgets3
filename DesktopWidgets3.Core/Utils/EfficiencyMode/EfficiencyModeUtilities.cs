﻿using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace DesktopWidgets3.Core.Utils.EfficiencyMode;

/// <summary>
/// Based on: <br/>
/// <see href="https://devblogs.microsoft.com/performance-diagnostics/reduce-process-interference-with-task-manager-efficiency-mode"/> <br/>
/// <see href="https://devblogs.microsoft.com/performance-diagnostics/introducing-ecoqos/"/> <br/>
/// Codes are edited from: https://github.com/HavenDV/H.NotifyIcon.
/// </summary>
public static class EfficiencyModeUtilities
{
    /// <summary>
    /// Enables/disables efficient mode for process <br/>
    /// Based on: <see href="https://devblogs.microsoft.com/performance-diagnostics/reduce-process-interference-with-task-manager-efficiency-mode/"/> 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows10.0.16299.0")]
    public static void SetEfficiencyMode(bool value)
    {
        var ecoLevel = Environment.OSVersion.Version >= new Version(11, 0)
#pragma warning disable CA1416 // Validate platform compatibility
            ? QualityOfServiceLevel.Eco
#pragma warning restore CA1416 // Validate platform compatibility
            : QualityOfServiceLevel.Low;

        SetProcessQualityOfServiceLevel(value
            ? ecoLevel
            : QualityOfServiceLevel.Default);
        SetProcessPriorityClass(value
            ? ProcessPriorityClass.Idle
            : ProcessPriorityClass.Default);
    }

    /// <summary>
    /// Based on <see href="https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation"/>
    /// </summary>
    [SupportedOSPlatform("windows10.0.16299.0")]
    private static unsafe void SetProcessQualityOfServiceLevel(QualityOfServiceLevel level)
    {
        var powerThrottling = new PROCESS_POWER_THROTTLING_STATE
        {
            Version = PInvoke.PROCESS_POWER_THROTTLING_CURRENT_VERSION
        };

        switch (level)
        {
            // Let system manage all power throttling. ControlMask is set to 0 as we don’t want 
            // to control any mechanisms.
            case QualityOfServiceLevel.Default:
                powerThrottling.ControlMask = 0;
                powerThrottling.StateMask = 0;
                break;

            // Turn EXECUTION_SPEED throttling on. 
            // ControlMask selects the mechanism and StateMask declares which mechanism should be on or off.
#pragma warning disable CA1416 // Validate platform compatibility
            case QualityOfServiceLevel.Eco when Environment.OSVersion.Version >= new Version(11, 0):
#pragma warning restore CA1416 // Validate platform compatibility
            case QualityOfServiceLevel.Low:
                powerThrottling.ControlMask = PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED;
                powerThrottling.StateMask = PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED;
                break;

            // Turn EXECUTION_SPEED throttling off. 
            // ControlMask selects the mechanism and StateMask is set to zero as mechanisms should be turned off.
            case QualityOfServiceLevel.High:
                powerThrottling.ControlMask = PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED;
                powerThrottling.StateMask = 0;
                break;

            default:
                throw new NotImplementedException();
        }

        _ = PInvoke.SetProcessInformation(
            hProcess: PInvoke.GetCurrentProcess(),
            ProcessInformationClass: PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
            ProcessInformation: &powerThrottling,
            ProcessInformationSize: (uint)sizeof(PROCESS_POWER_THROTTLING_STATE)).EnsureNonZero();
    }

    /// <summary>
    /// Based on <see href="https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass"/>
    /// </summary>
    [SupportedOSPlatform("windows5.1.2600")]
    private static unsafe void SetProcessPriorityClass(ProcessPriorityClass priorityClass)
    {
        var flags = priorityClass switch
        {
            ProcessPriorityClass.Default => PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS,
            ProcessPriorityClass.Idle => PROCESS_CREATION_FLAGS.IDLE_PRIORITY_CLASS,
            ProcessPriorityClass.BelowNormal => PROCESS_CREATION_FLAGS.BELOW_NORMAL_PRIORITY_CLASS,
            ProcessPriorityClass.Normal => PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS,
            ProcessPriorityClass.AboveNormal => PROCESS_CREATION_FLAGS.ABOVE_NORMAL_PRIORITY_CLASS,
            ProcessPriorityClass.High => PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS,
            ProcessPriorityClass.Realtime => PROCESS_CREATION_FLAGS.REALTIME_PRIORITY_CLASS,
            _ => throw new NotImplementedException(),
        };

        _ = PInvoke.SetPriorityClass(
            hProcess: PInvoke.GetCurrentProcess(),
            dwPriorityClass: flags).EnsureNonZero();
    }
}
