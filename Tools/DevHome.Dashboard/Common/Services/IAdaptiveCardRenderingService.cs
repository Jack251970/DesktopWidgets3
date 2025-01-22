// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Dashboard.Common.Services;

public interface IAdaptiveCardRenderingService
{
    public Task<AdaptiveCardRenderer> GetRendererAsync();

    public event EventHandler RendererUpdated;
}
