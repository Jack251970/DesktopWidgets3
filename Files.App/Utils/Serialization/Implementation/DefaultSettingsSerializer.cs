// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Serialization.Implementation;

internal sealed class DefaultSettingsSerializer : ISettingsSerializer
{
	/*private string? _filePath;*/

	public bool CreateFile(string path)
	{
        // CHANGE: Remove function to create & read and write setting file.
        /*PInvoke.CreateDirectoryFromApp(Path.GetDirectoryName(path), null);

		var hFile = CreateFileFromApp(path, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_ALWAYS, (uint)File_Attributes.BackupSemantics, IntPtr.Zero);
		if (hFile.IsHandleInvalid())
		{
			return false;
		}

		Win32PInvoke.CloseHandle(hFile);

        _filePath = path;*/
        return true;
	}

	/// <summary>
	/// Reads a file to a string
	/// </summary>
	/// <returns>A string value or string.Empty if nothing is present in the file</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public string ReadFromFile()
	{
        // CHANGE: Remove function to create & read and write setting file.
        /*_ = _filePath ?? throw new ArgumentNullException(nameof(_filePath));

        return Win32Helper.ReadStringFromFile(_filePath);*/
        return string.Empty;
    }

	public bool WriteToFile(string? text)
	{
        // CHANGE: Remove function to create & read and write setting file.
        /*_ = _filePath ?? throw new ArgumentNullException(null, nameof(_filePath));

        return Win32Helper.WriteStringToFile(_filePath, text!);*/
        return true;
    }
}
