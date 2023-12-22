using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Files.App.Helpers;
using Files.App.Utils.Shell;
using Files.Core.Data.Items;
using Files.Shared.Extensions;
using Files.Shared.Helpers;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Utils.Storage;

public class FileOperationsHelpers
{
    private static readonly Ole32.PROPERTYKEY PKEY_FilePlaceholderStatus = new(new Guid("B2F9B9D6-FEC4-4DD5-94D7-8957488C807B"), 2);
    private const uint PS_CLOUDFILE_PLACEHOLDER = 8;

    private static ProgressHandler? progressHandler; // Warning: must be initialized from a MTA thread

    #region parse shortcut

    public static async Task<ShellLinkItem?> ParseLinkAsync(string linkPath)
    {
        if (string.IsNullOrEmpty(linkPath))
        {
            return null;
        }

        var targetPath = string.Empty;

        try
        {
            if (FileExtensionHelpers.IsShortcutFile(linkPath))
            {
                using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, default, TimeSpan.FromMilliseconds(100));
                targetPath = link.TargetPath;
                return ShellFolderExtensions.GetShellLinkItem(link);
            }
            else if (FileExtensionHelpers.IsWebLinkFile(linkPath))
            {
                targetPath = await Win32API.StartSTATask(() =>
                {
                    var ipf = new Url.IUniformResourceLocator();
                    ((System.Runtime.InteropServices.ComTypes.IPersistFile)ipf).Load(linkPath, 0);
                    ipf.GetUrl(out var retVal);
                    return retVal;
                });
                return string.IsNullOrEmpty(targetPath) ?
                    new ShellLinkItem
                    {
                        TargetPath = string.Empty,
                        InvalidTarget = true
                    } : new ShellLinkItem { TargetPath = targetPath };
            }
            return null;
        }
        catch (FileNotFoundException) // Could not parse shortcut
        {
            // Return a item containing the invalid target path
            return new ShellLinkItem
            {
                TargetPath = string.IsNullOrEmpty(targetPath) ? string.Empty : targetPath,
                InvalidTarget = true
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region delete item

    public static Task<(bool, ShellOperationResult)> TestRecycleAsync(string[] fileToDeletePath)
    {
        return Win32API.StartSTATask(async () =>
        {
            using var op = new ShellFileOperations2();

            op.Options = ShellFileOperations.OperationFlags.Silent
                        | ShellFileOperations.OperationFlags.NoConfirmation
                        | ShellFileOperations.OperationFlags.NoErrorUI;
            op.Options |= ShellFileOperations.OperationFlags.RecycleOnDelete;

            var shellOperationResult = new ShellOperationResult();
            var tryDelete = false;

            for (var i = 0; i < fileToDeletePath.Length; i++)
            {
                if (!SafetyExtensions.IgnoreExceptions(() =>
                {
                    using var shi = new ShellItem(fileToDeletePath[i]);
                    using var file = SafetyExtensions.IgnoreExceptions(() => GetFirstFile(shi)) ?? shi;
                    if (file.Properties.GetProperty<uint>(PKEY_FilePlaceholderStatus) == PS_CLOUDFILE_PLACEHOLDER)
                    {
                        // Online only files cannot be tried for deletion, so they are treated as to be permanently deleted.
                        shellOperationResult.Items.Add(new ShellOperationItemResult()
                        {
                            Succeeded = false,
                            Source = fileToDeletePath[i],
                            HResult = HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
                        });
                    }
                    else
                    {
                        op.QueueDeleteOperation(file);
                        tryDelete = true;
                    }
                }))
                {
                    shellOperationResult.Items.Add(new ShellOperationItemResult()
                    {
                        Succeeded = false,
                        Source = fileToDeletePath[i],
                        HResult = -1
                    });
                }
            }

            if (!tryDelete)
            {
                return (true, shellOperationResult);
            }

            var deleteTcs = new TaskCompletionSource<bool>();
            op.PreDeleteItem += [DebuggerHidden] (s, e) =>
            {
                if (!e.Flags.HasFlag(ShellFileOperations.TransferFlags.DeleteRecycleIfPossible))
                {
                    shellOperationResult.Items.Add(new ShellOperationItemResult()
                    {
                        Succeeded = false,
                        Source = e.SourceItem.GetParsingPath(),
                        HResult = HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
                    });
                    throw new Win32Exception(HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND); // E_FAIL, stops operation
                }
                else
                {
                    shellOperationResult.Items.Add(new ShellOperationItemResult()
                    {
                        Succeeded = true,
                        Source = e.SourceItem.GetParsingPath(),
                        HResult = HRESULT.COPYENGINE_E_USER_CANCELLED
                    });
                    throw new Win32Exception(HRESULT.COPYENGINE_E_USER_CANCELLED); // E_FAIL, stops operation
                }
            };
            op.FinishOperations += (s, e) => deleteTcs.TrySetResult(e.Result.Succeeded);

            try
            {
                op.PerformOperations();
            }
            catch
            {
                deleteTcs.TrySetResult(false);
            }

            return (await deleteTcs.Task, shellOperationResult);
        });
    }

    private static ShellItem? GetFirstFile(ShellItem shi)
    {
        if (!shi.IsFolder || shi.Attributes.HasFlag(ShellItemAttribute.Stream))
        {
            return shi;
        }
        using var shf = new ShellFolder(shi);
        if (shf.FirstOrDefault(x => !x.IsFolder || x.Attributes.HasFlag(ShellItemAttribute.Stream)) is ShellItem item)
        {
            return item;
        }
        foreach (var shsfi in shf.Where(x => x.IsFolder && !x.Attributes.HasFlag(ShellItemAttribute.Stream)))
        {
            using var shsf = new ShellFolder(shsfi);
            if (GetFirstFile(shsf) is ShellItem item2)
            {
                return item2;
            }
        }
        return null;
    }

    #endregion

    #region rename item

    public static Task<(bool, ShellOperationResult)> RenameItemAsync(string fileToRenamePath, string newName, bool overwriteOnRename, long ownerHwnd, bool asAdmin, string operationID = "")
    {
        operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

        progressHandler ??= new();

        return Win32API.StartSTATask(async () =>
        {
            using var op = new ShellFileOperations2();
            var shellOperationResult = new ShellOperationResult();

            op.Options = ShellFileOperations.OperationFlags.Silent
                      | ShellFileOperations.OperationFlags.NoErrorUI;
            if (asAdmin)
            {
                op.Options |= ShellFileOperations.OperationFlags.ShowElevationPrompt
                        | ShellFileOperations.OperationFlags.RequireElevation;
            }
            op.OwnerWindow = (IntPtr)ownerHwnd;
            op.Options |= !overwriteOnRename ? ShellFileOperations.OperationFlags.RenameOnCollision : 0;

            if (!SafetyExtensions.IgnoreExceptions(() =>
            {
                using var shi = new ShellItem(fileToRenamePath);
                op.QueueRenameOperation(shi, newName);
            }))
            {
                shellOperationResult.Items.Add(new ShellOperationItemResult()
                {
                    Succeeded = false,
                    Source = fileToRenamePath,
                    HResult = -1
                });
            }

            progressHandler.OwnerWindow = op.OwnerWindow;
            progressHandler.AddOperation(operationID);

            var renameTcs = new TaskCompletionSource<bool>();
            op.PostRenameItem += (s, e) =>
            {
                shellOperationResult.Items.Add(new ShellOperationItemResult()
                {
                    Succeeded = e.Result.Succeeded,
                    Source = e.SourceItem.GetParsingPath(),
                    Destination = !string.IsNullOrEmpty(e.Name) ? Path.Combine(Path.GetDirectoryName(e.SourceItem.GetParsingPath())!, e.Name) : null!,
                    HResult = (int)e.Result
                });
            };
            //op.PostRenameItem += (_, e) => UpdateFileTagsDb(e, "rename");
            op.FinishOperations += (s, e) => renameTcs.TrySetResult(e.Result.Succeeded);

            try
            {
                op.PerformOperations();
            }
            catch
            {
                renameTcs.TrySetResult(false);
            }

            progressHandler.RemoveOperation(operationID);

            return (await renameTcs.Task, shellOperationResult);
        });
    }

    #endregion

    #region check file in use

    public static IEnumerable<Win32Process>? CheckFileInUse(string[] fileToCheckPath)
    {
        var processes = SafetyExtensions.IgnoreExceptions(() => FileUtils.WhoIsLocking(fileToCheckPath), null);

        if (processes is not null)
        {
            var win32proc = processes.Select(x => new Win32Process()
            {
                Name = x.ProcessName,
                Pid = x.Id,
                FileName = SafetyExtensions.IgnoreExceptions(() => x.MainModule?.FileName)!,
                AppName = SafetyExtensions.IgnoreExceptions(() => x.MainModule?.FileVersionInfo?.FileDescription)!
            }).ToList();
            processes.ForEach(x => x.Dispose());

            return win32proc!;
        }
        else
        {
            return null;
        }
    }

    #endregion

    private class ProgressHandler : Disposable
    {
        private readonly ManualResetEvent operationsCompletedEvent;

        public class OperationWithProgress
        {
            public double Progress
            {
                get; set;
            }
            public bool Canceled
            {
                get; set;
            }
        }

        private readonly Shell32.ITaskbarList4? taskbar;
        private readonly ConcurrentDictionary<string, OperationWithProgress> operations;

        public HWND OwnerWindow
        {
            get; set;
        }

        public ProgressHandler()
        {
            taskbar = Win32API.CreateTaskbarObject();
            operations = new ConcurrentDictionary<string, OperationWithProgress>();
            operationsCompletedEvent = new ManualResetEvent(true);
        }

        public int Progress
        {
            get
            {
                var ongoing = operations.ToArray().Where(x => !x.Value.Canceled);
                return ongoing.Any() ? (int)ongoing.Average(x => x.Value.Progress) : 0;
            }
        }

        public void AddOperation(string uid)
        {
            operations.TryAdd(uid, new OperationWithProgress());
            UpdateTaskbarProgress();
            operationsCompletedEvent.Reset();
        }

        public void RemoveOperation(string uid)
        {
            operations.TryRemove(uid, out _);
            UpdateTaskbarProgress();
            if (!operations.Any())
            {
                operationsCompletedEvent.Set();
            }
        }

        public void UpdateOperation(string uid, double progress)
        {
            if (operations.TryGetValue(uid, out var op))
            {
                op.Progress = progress;
                UpdateTaskbarProgress();
            }
        }

        public bool CheckCanceled(string uid)
        {
            return !operations.TryGetValue(uid, out var op) || op.Canceled;
        }

        public void TryCancel(string uid)
        {
            if (operations.TryGetValue(uid, out var op))
            {
                op.Canceled = true;
                UpdateTaskbarProgress();
            }
        }

        private void UpdateTaskbarProgress()
        {
            if (OwnerWindow == HWND.NULL || taskbar is null)
            {
                return;
            }
            if (operations.Any())
            {
                taskbar.SetProgressValue(OwnerWindow, (ulong)Progress, 100);
            }
            else
            {
                taskbar.SetProgressState(OwnerWindow, Shell32.TBPFLAG.TBPF_NOPROGRESS);
            }
        }

        public void WaitForCompletion()
        {
            operationsCompletedEvent.WaitOne();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                operationsCompletedEvent?.Dispose();
                if (taskbar is not null)
                    Marshal.ReleaseComObject(taskbar);
            }
        }
    }
}
