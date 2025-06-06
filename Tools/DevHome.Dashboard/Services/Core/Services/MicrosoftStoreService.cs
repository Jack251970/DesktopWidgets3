﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Runtime.InteropServices;
using DevHome.Dashboard.Services.Core.Contracts;
using Serilog;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;

namespace DevHome.Dashboard.Services.Core.Services;

/// <summary>
/// Service class for using the Microsoft Store API
/// https://learn.microsoft.com/uwp/api/windows.applicationmodel.store.preview?view=winrt-22621
/// </summary>
public class MicrosoftStoreService : IMicrosoftStoreService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(MicrosoftStoreService));

    private readonly AppInstallManager _appInstallManager = new();
    private readonly TimeSpan _storeInstallTimeout = TimeSpan.FromMinutes(1);

    public event TypedEventHandler<AppInstallManager, AppInstallManagerItemEventArgs> ItemCompleted
    {
        add => _appInstallManager.ItemCompleted += value;
        remove => _appInstallManager.ItemCompleted -= value;
    }

    public event TypedEventHandler<AppInstallManager, AppInstallManagerItemEventArgs> ItemStatusChanged
    {
        add => _appInstallManager.ItemStatusChanged += value;
        remove => _appInstallManager.ItemStatusChanged -= value;
    }

    public async Task<bool> IsAppUpdateAvailableAsync(string productId)
    {
        return await SearchForUpdateAsync(productId, new AppUpdateOptions
        {
            AutomaticallyDownloadAndInstallUpdateIfFound = false,
        });
    }

    public async Task<bool> StartAppUpdateAsync(string productId)
    {
        return await SearchForUpdateAsync(productId, new AppUpdateOptions
        {
            AutomaticallyDownloadAndInstallUpdateIfFound = true,
        });
    }

    /// <summary>
    /// Search for an update for the specified product id
    /// </summary>
    /// <param name="productId">Target product id</param>
    /// <param name="options">Update option</param>
    /// <returns>True if an update is available, false otherwise.</returns>
    /// <exception cref="COMException">Throws exception if operation failed (e.g. product id was not found)</exception>
    private async Task<bool> SearchForUpdateAsync(string productId, AppUpdateOptions options)
    {
        var appInstallItem = await _appInstallManager.SearchForUpdatesAsync(
            productId,
            skuId: null,
            correlationVector: null,
            clientId: null,
            options);

        // Check if update is available
        return appInstallItem != null;
    }

    public async Task<bool> TryInstallPackageAsync(string packageId)
    {
        try
        {
            // Wait for a maximum of StoreInstallTimeout (60 seconds).
            await InstallPackageAsync(packageId, _storeInstallTimeout);

            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Package installation failed");
        }

        return false;
    }

    public async Task InstallPackageAsync(string packageId, TimeSpan timeout)
    {
        var installTask = InstallPackageAsync(packageId);

        var completedTask = await Task.WhenAny(installTask, Task.Delay(timeout));

        if (completedTask.Exception != null)
        {
            throw completedTask.Exception;
        }

        if (completedTask != installTask)
        {
            throw new TimeoutException("Store Install task did not finish in time.");
        }
    }

    private async Task InstallPackageAsync(string packageId)
    {
        await Task.Run(() =>
        {
            var tcs = new TaskCompletionSource<bool>();
            AppInstallItem installItem;
            try
            {
                _log.Information($"Starting {packageId} install");
                installItem = _appInstallManager.StartAppInstallAsync(packageId, null, true, false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Error($"{packageId} install failure");
                tcs.SetException(ex);
                return tcs.Task;
            }

            installItem.Completed += (sender, args) =>
            {
                if (!tcs.TrySetResult(true))
                {
                    _log.Information($"{packageId} In Completed handler, RanToCompleted already set.");
                }
                else
                {
                    _log.Information($"{packageId} In Completed handler, RanToCompleted set.");
                }
            };

            installItem.StatusChanged += (sender, args) =>
            {
                if (installItem.GetCurrentStatus().InstallState == AppInstallState.Canceled
                    || installItem.GetCurrentStatus().InstallState == AppInstallState.Error)
                {
                    tcs.TrySetException(new JobFailedException(installItem.GetCurrentStatus().ErrorCode.ToString()));
                }
                else if (installItem.GetCurrentStatus().InstallState == AppInstallState.Completed)
                {
                    if (!tcs.TrySetResult(true))
                    {
                        _log.Information($"{packageId} In StatusChanged handler, RanToCompleted already set.");
                    }
                    else
                    {
                        _log.Information($"{packageId} In StatusChanged handler, RanToCompleted set.");
                    }
                }
            };
            return tcs.Task;
        });
    }
}
