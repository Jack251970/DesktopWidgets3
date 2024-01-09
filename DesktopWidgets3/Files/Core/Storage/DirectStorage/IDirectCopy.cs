using DesktopWidgets3.Files.Core.Storage.ModifiableStorage;
using DesktopWidgets3.Files.Core.Storage.NestedStorage;

namespace DesktopWidgets3.Files.Core.Storage.DirectStorage;

/// <summary>
/// Provides direct copy operation of storage objects.
/// </summary>
public interface IDirectCopy : IModifiableFolder
{
    /// <summary>
    /// Creates a copy of the provided storable item in this folder.
    /// </summary>
    Task<INestedStorable> CreateCopyOfAsync(INestedStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default);
}
