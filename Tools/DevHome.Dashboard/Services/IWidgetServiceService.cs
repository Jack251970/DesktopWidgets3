﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using static DevHome.Dashboard.Services.WidgetServiceService;

namespace DevHome.Dashboard.Services;

public interface IWidgetServiceService
{
    public Task<bool> TryInstallingWidgetService();

    public WidgetServiceStates GetWidgetServiceState();
}
