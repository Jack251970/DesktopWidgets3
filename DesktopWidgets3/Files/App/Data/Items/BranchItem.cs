// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace DesktopWidgets3.Files.App.Data.Items;

public record BranchItem(string Name, bool IsHead, bool IsRemote, int? AheadBy, int? BehindBy);