﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services.Settings;

public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
{
	int DefaultGridViewSize { get; set; }
}
