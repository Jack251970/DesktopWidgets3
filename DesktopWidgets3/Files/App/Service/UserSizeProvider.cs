// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Core.Services.SizeProvider;

namespace DesktopWidgets3.Files.App.Services;

public class UserSizeProvider : ISizeProvider
{
    private readonly ISizeProvider provider;

    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    public UserSizeProvider()
    {
        provider = GetProvider();
        provider.SizeChanged += Provider_SizeChanged;
    }

    public Task CleanAsync()
        => provider.CleanAsync();

    public async Task ClearAsync()
        => await provider.ClearAsync();

    public Task UpdateAsync(string path, CancellationToken cancellationToken)
        => provider.UpdateAsync(path, cancellationToken);

    public bool TryGetSize(string path, out ulong size)
        => provider.TryGetSize(path, out size);

    public void Dispose()
    {
        provider.Dispose();
    }

    private ISizeProvider GetProvider() => new NoSizeProvider();

    private void Provider_SizeChanged(object? sender, SizeChangedEventArgs e)
        => SizeChanged?.Invoke(this, e);
}
