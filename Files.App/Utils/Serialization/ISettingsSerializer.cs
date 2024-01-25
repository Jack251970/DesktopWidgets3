// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Serialization;

// TODO: change to internal.
public interface ISettingsSerializer
{
	bool CreateFile(string path);

	string ReadFromFile();

	bool WriteToFile(string? text);
}
