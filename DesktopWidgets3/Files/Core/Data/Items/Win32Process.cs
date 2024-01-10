// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Items;

public class Win32Process
{
    public string Name { get; set; } = null!;

    public int Pid { get; set; }

    public string FileName { get; set; } = null!;

    public string AppName { get; set; } = null!;
}