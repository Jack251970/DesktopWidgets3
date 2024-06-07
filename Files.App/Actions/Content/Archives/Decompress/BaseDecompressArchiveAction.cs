﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions;

internal abstract class BaseDecompressArchiveAction : BaseUIAction, IAction
{
	protected readonly IContentPageContext context;

	public abstract string Label { get; }

	public abstract string Description { get; }

	public virtual HotKey HotKey
		=> HotKey.None;

	public override bool IsExecutable =>
		(IsContextPageTypeAdaptedToCommand() &&
		CompressHelper.CanDecompress(context.SelectedItems) ||
		CanDecompressInsideArchive()) &&
		FolderViewViewModel.CanShowDialog;

	public BaseDecompressArchiveAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(folderViewViewModel)
    {
        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public abstract Task ExecuteAsync(object? parameter = null);

	protected bool IsContextPageTypeAdaptedToCommand()
	{
		return
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder &&
			context.PageType != ContentPageTypes.None;
	}

	protected virtual bool CanDecompressInsideArchive()
	{
		return false;
	}

	protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.SelectedItems):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
