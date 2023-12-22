// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.App.Helpers;
using Files.App.Utils.Shell;
using Files.App.Utils.Storage;
using Files.Core.Data.Enums;
using LibGit2Sharp;

namespace Files.App.Utils.Git;

internal static class GitHelpers
{
    private static ThreadWithMessageQueue? _owningThread;

    private static int _activeOperationsCount = 0;

    public static bool IsRepositoryEx(string path, out string repoRootPath)
    {
        repoRootPath = path;

        var rootPath = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(rootPath))
        {
            return false;
        }

        var repositoryRootPath = GetGitRepositoryPath(path, rootPath);
        if (string.IsNullOrEmpty(repositoryRootPath))
        {
            return false;
        }

        if (Repository.IsValid(repositoryRootPath))
        {
            repoRootPath = repositoryRootPath;
            return true;
        }

        return false;
    }

    public static string? GetGitRepositoryPath(string? path, string root)
    {
        if (string.IsNullOrEmpty(root))
        {
            return null;
        }

        if (root.EndsWith('\\'))
        {
            root = root[..^1];
        }

        if (string.IsNullOrWhiteSpace(path) ||
            path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
            path.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
            ShellStorageFolder.IsShellPath(path))
        {
            return null;
        }

        try
        {
            return Repository.IsValid(path) ? path : GetGitRepositoryPath(PathNormalization.GetParentDir(path), root);
        }
        catch (LibGit2SharpException)
        {
            return null;
        }
    }

    public static async Task<BranchItem?> GetRepositoryHead(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Repository.IsValid(path))
        {
            return null;
        }

        var (_, returnValue) = await PostMethodToThreadWithMessageQueueAsync<(GitOperationResult, BranchItem?)>(() =>
        {
            BranchItem? head = null;
            try
            {
                using var repository = new Repository(path);
                var branch = GetValidBranches(repository.Branches).FirstOrDefault(b => b.IsCurrentRepositoryHead);
                if (branch is not null)
                {
                    head = new BranchItem(
                        branch.FriendlyName,
                        branch.IsCurrentRepositoryHead,
                        branch.IsRemote,
                        TryGetTrackingDetails(branch)?.AheadBy ?? 0,
                        TryGetTrackingDetails(branch)?.BehindBy ?? 0
                    );
                }
            }
            catch
            {
                return (GitOperationResult.GenericError, head);
            }

            return (GitOperationResult.Success, head);
        });

        return returnValue!;
    }

    private static IEnumerable<Branch> GetValidBranches(BranchCollection branches)
    {
        foreach (var branch in branches)
        {
            try
            {
                var throwIfInvalid = branch.IsCurrentRepositoryHead;
            }
            catch (LibGit2SharpException)
            {
                continue;
            }

            yield return branch;
        }
    }

    private static BranchTrackingDetails? TryGetTrackingDetails(Branch branch)
    {
        try
        {
            return branch.TrackingDetails;
        }
        catch (LibGit2SharpException)
        {
            return null;
        }
    }

    private static async Task<T?> PostMethodToThreadWithMessageQueueAsync<T>(Func<object> payload)
    {
        T? returnValue = default;

        Interlocked.Increment(ref _activeOperationsCount);
        _owningThread ??= new ThreadWithMessageQueue();

        try
        {
            returnValue = await _owningThread.PostMethod<T>(payload);
        }
        finally
        {
            DisposeIfFinished();
        }

        return returnValue;
    }

    private static void DisposeIfFinished()
    {
        if (Interlocked.Decrement(ref _activeOperationsCount) == 0)
        {
            TryDispose();
        }
    }

    public static void TryDispose()
    {
        var threadToDispose = _owningThread;
        _owningThread = null;
        Interlocked.Exchange(ref _activeOperationsCount, 0);
        threadToDispose?.Dispose();
    }
}