﻿namespace DesktopWidgets3.Helpers;

/// <summary>
/// Provides static extension for path extension.
/// </summary>
public class FileExtensionHelpers
{
    /// <summary>
    /// Check if the file extension matches one of the specified extensions.
    /// </summary>
    /// <param name="filePathToCheck">Path or name or extension of the file to check.</param>
    /// <param name="extensions">List of the extensions to check.</param>
    /// <returns><c>true</c> if the filePathToCheck has one of the specified extensions; otherwise, <c>false</c>.</returns>
    public static bool HasExtension(string? filePathToCheck, params string[] extensions)
    {
        return !string.IsNullOrWhiteSpace(filePathToCheck) && extensions.Any(ext => filePathToCheck.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if the file extension is a vhd disk file.
    /// </summary>
    /// <param name="fileExtensionToCheck">The file extension to check.</param>
    /// <returns><c>true</c> if the fileExtensionToCheck is a vhd disk file; otherwise, <c>false</c>.</returns>
    /// <remarks>Vhd disk file types are; vhd, vhdx</remarks>
    public static bool IsVhdFile(string? fileExtensionToCheck)
    {
        return HasExtension(fileExtensionToCheck, ".vhd", ".vhdx");
    }

    public static bool IsShortcutFile(string? filePathToCheck)
    {
        return HasExtension(filePathToCheck, ".lnk");
    }

    public static bool IsShortcutOrUrlFile(string? filePathToCheck)
    {
        return HasExtension(filePathToCheck, ".lnk", ".url");
    }

    /// <summary>
    /// Check if the file extension is a screen saver file.
    /// </summary>
    /// <param name="fileExtensionToCheck">The file extension to check.</param>
    /// <returns><c>true</c> if the fileExtensionToCheck is a screen saver file; otherwise, <c>false</c>.</returns>
    /// <remarks>Screen saver file types are; scr</remarks>
    public static bool IsScreenSaverFile(string? fileExtensionToCheck)
    {
        return HasExtension(fileExtensionToCheck, ".scr");
    }

    /// <summary>
    /// Check if the file path is a web link file.
    /// </summary>
    /// <param name="filePathToCheck">The file path to check.</param>
    /// <returns><c>true</c> if the filePathToCheck is a web link file; otherwise, <c>false</c>.</returns>
    /// <remarks>Web link file type is .url</remarks>
    public static bool IsWebLinkFile(string? filePathToCheck)
    {
        return HasExtension(filePathToCheck, ".url");
    }

    /// <summary>
    /// Check if the file path is an msi installer file.
    /// </summary>
    /// <param name="filePathToCheck">The file path to check.</param>
    /// <returns><c>true</c> if the filePathToCheck is an msi installer file; otherwise, <c>false</c>.</returns>
    public static bool IsMsiFile(string? filePathToCheck)
    {
        return HasExtension(filePathToCheck, ".msi");
    }
}
