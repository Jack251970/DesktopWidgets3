// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Actions;
using Microsoft.AppCenter.Analytics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Immutable;

namespace Files.App.Data.Commands;

internal class CommandManager : ICommandManager
{
    private IFolderViewViewModel FolderViewViewModel = null!;

    private IGeneralSettingsService Settings { get; set; } = null!;

    private IImmutableDictionary<CommandCodes, IRichCommand> commands = null!;
	private IImmutableDictionary<HotKey, IRichCommand> hotKeys = new Dictionary<HotKey, IRichCommand>().ToImmutableDictionary();

	public IRichCommand this[CommandCodes code] => commands.TryGetValue(code, out var command) ? command : None;
	public IRichCommand this[string code]
	{
		get
		{
			try
			{
				return commands[Enum.Parse<CommandCodes>(code, true)];
			}
			catch
			{
				return None;
			}
		}
	}
	public IRichCommand this[HotKey hotKey]
		=> hotKeys.TryGetValue(hotKey with { IsVisible = true }, out var command) ? command
		: hotKeys.TryGetValue(hotKey with { IsVisible = false }, out command) ? command
		: None;

	public IRichCommand None => commands[CommandCodes.None];
    public IRichCommand OpenHelp => commands[CommandCodes.OpenHelp];
	public IRichCommand ToggleFullScreen => commands[CommandCodes.ToggleFullScreen];
	public IRichCommand EnterCompactOverlay => commands[CommandCodes.EnterCompactOverlay];
	public IRichCommand ExitCompactOverlay => commands[CommandCodes.ExitCompactOverlay];
	public IRichCommand ToggleCompactOverlay => commands[CommandCodes.ToggleCompactOverlay];
    public IRichCommand Search => commands[CommandCodes.Search];
	public IRichCommand EditPath => commands[CommandCodes.EditPath];
	public IRichCommand Redo => commands[CommandCodes.Redo];
	public IRichCommand Undo => commands[CommandCodes.Undo];
	public IRichCommand ToggleShowHiddenItems => commands[CommandCodes.ToggleShowHiddenItems];
	public IRichCommand ToggleShowFileExtensions => commands[CommandCodes.ToggleShowFileExtensions];
	public IRichCommand TogglePreviewPane => commands[CommandCodes.TogglePreviewPane];
	public IRichCommand ToggleDetailsPane => commands[CommandCodes.ToggleDetailsPane];
	public IRichCommand ToggleInfoPane => commands[CommandCodes.ToggleInfoPane];
	public IRichCommand SelectAll => commands[CommandCodes.SelectAll];
	public IRichCommand InvertSelection => commands[CommandCodes.InvertSelection];
	public IRichCommand ClearSelection => commands[CommandCodes.ClearSelection];
	public IRichCommand ToggleSelect => commands[CommandCodes.ToggleSelect];
	public IRichCommand ShareItem => commands[CommandCodes.ShareItem];
	public IRichCommand EmptyRecycleBin => commands[CommandCodes.EmptyRecycleBin];
	public IRichCommand RestoreRecycleBin => commands[CommandCodes.RestoreRecycleBin];
	public IRichCommand RestoreAllRecycleBin => commands[CommandCodes.RestoreAllRecycleBin];
	public IRichCommand RefreshItems => commands[CommandCodes.RefreshItems];
	public IRichCommand Rename => commands[CommandCodes.Rename];
	public IRichCommand CreateShortcut => commands[CommandCodes.CreateShortcut];
	public IRichCommand CreateShortcutFromDialog => commands[CommandCodes.CreateShortcutFromDialog];
	public IRichCommand CreateFolder => commands[CommandCodes.CreateFolder];
	public IRichCommand CreateFolderWithSelection => commands[CommandCodes.CreateFolderWithSelection];
	public IRichCommand AddItem => commands[CommandCodes.AddItem];
	public IRichCommand PinToStart => commands[CommandCodes.PinToStart];
	public IRichCommand UnpinFromStart => commands[CommandCodes.UnpinFromStart];
	public IRichCommand PinItemToFavorites => commands[CommandCodes.PinItemToFavorites];
	public IRichCommand UnpinItemFromFavorites => commands[CommandCodes.UnpinItemFromFavorites];
	public IRichCommand SetAsWallpaperBackground => commands[CommandCodes.SetAsWallpaperBackground];
	public IRichCommand SetAsSlideshowBackground => commands[CommandCodes.SetAsSlideshowBackground];
	public IRichCommand SetAsLockscreenBackground => commands[CommandCodes.SetAsLockscreenBackground];
	public IRichCommand CopyItem => commands[CommandCodes.CopyItem];
	public IRichCommand CopyPath => commands[CommandCodes.CopyPath];
	public IRichCommand CopyPathWithQuotes => commands[CommandCodes.CopyPathWithQuotes];
	public IRichCommand CutItem => commands[CommandCodes.CutItem];
	public IRichCommand PasteItem => commands[CommandCodes.PasteItem];
	public IRichCommand PasteItemToSelection => commands[CommandCodes.PasteItemToSelection];
	public IRichCommand DeleteItem => commands[CommandCodes.DeleteItem];
	public IRichCommand DeleteItemPermanently => commands[CommandCodes.DeleteItemPermanently];
    public IRichCommand InstallFont => commands[CommandCodes.InstallFont];
	public IRichCommand InstallInfDriver => commands[CommandCodes.InstallInfDriver];
	public IRichCommand InstallCertificate => commands[CommandCodes.InstallCertificate];
	public IRichCommand RunAsAdmin => commands[CommandCodes.RunAsAdmin];
	public IRichCommand RunAsAnotherUser => commands[CommandCodes.RunAsAnotherUser];
	public IRichCommand RunWithPowershell => commands[CommandCodes.RunWithPowershell];
	public IRichCommand LaunchPreviewPopup => commands[CommandCodes.LaunchPreviewPopup];
	public IRichCommand CompressIntoArchive => commands[CommandCodes.CompressIntoArchive];
	public IRichCommand CompressIntoSevenZip => commands[CommandCodes.CompressIntoSevenZip];
	public IRichCommand CompressIntoZip => commands[CommandCodes.CompressIntoZip];
	public IRichCommand DecompressArchive => commands[CommandCodes.DecompressArchive];
	public IRichCommand DecompressArchiveHere => commands[CommandCodes.DecompressArchiveHere];
	public IRichCommand DecompressArchiveHereSmart => commands[CommandCodes.DecompressArchiveHereSmart];
	public IRichCommand DecompressArchiveToChildFolder => commands[CommandCodes.DecompressArchiveToChildFolder];
    public IRichCommand RotateLeft => commands[CommandCodes.RotateLeft];
	public IRichCommand RotateRight => commands[CommandCodes.RotateRight];
    public IRichCommand OpenItem => commands[CommandCodes.OpenItem];
	public IRichCommand OpenItemWithApplicationPicker => commands[CommandCodes.OpenItemWithApplicationPicker];
	public IRichCommand OpenParentFolder => commands[CommandCodes.OpenParentFolder];
    public IRichCommand OpenInVSCode => commands[CommandCodes.OpenInVSCode];
	public IRichCommand OpenRepoInVSCode => commands[CommandCodes.OpenRepoInVSCode];
	public IRichCommand OpenProperties => commands[CommandCodes.OpenProperties];
	public IRichCommand OpenSettings => commands[CommandCodes.OpenSettings];
	public IRichCommand OpenTerminal => commands[CommandCodes.OpenTerminal];
	public IRichCommand OpenTerminalAsAdmin => commands[CommandCodes.OpenTerminalAsAdmin];
	public IRichCommand OpenCommandPalette => commands[CommandCodes.OpenCommandPalette];
	public IRichCommand LayoutDecreaseSize => commands[CommandCodes.LayoutDecreaseSize];
	public IRichCommand LayoutIncreaseSize => commands[CommandCodes.LayoutIncreaseSize];
	public IRichCommand LayoutDetails => commands[CommandCodes.LayoutDetails];
    public IRichCommand LayoutList => commands[CommandCodes.LayoutList];
    public IRichCommand LayoutTiles => commands[CommandCodes.LayoutTiles];
	public IRichCommand LayoutGridSmall => commands[CommandCodes.LayoutGridSmall];
	public IRichCommand LayoutGridMedium => commands[CommandCodes.LayoutGridMedium];
	public IRichCommand LayoutGridLarge => commands[CommandCodes.LayoutGridLarge];
	public IRichCommand LayoutColumns => commands[CommandCodes.LayoutColumns];
	public IRichCommand LayoutAdaptive => commands[CommandCodes.LayoutAdaptive];
	public IRichCommand SortByName => commands[CommandCodes.SortByName];
	public IRichCommand SortByDateModified => commands[CommandCodes.SortByDateModified];
	public IRichCommand SortByDateCreated => commands[CommandCodes.SortByDateCreated];
	public IRichCommand SortBySize => commands[CommandCodes.SortBySize];
	public IRichCommand SortByType => commands[CommandCodes.SortByType];
	public IRichCommand SortBySyncStatus => commands[CommandCodes.SortBySyncStatus];
	public IRichCommand SortByTag => commands[CommandCodes.SortByTag];
	public IRichCommand SortByPath => commands[CommandCodes.SortByPath];
	public IRichCommand SortByOriginalFolder => commands[CommandCodes.SortByOriginalFolder];
	public IRichCommand SortByDateDeleted => commands[CommandCodes.SortByDateDeleted];
	public IRichCommand SortAscending => commands[CommandCodes.SortAscending];
	public IRichCommand SortDescending => commands[CommandCodes.SortDescending];
	public IRichCommand ToggleSortDirection => commands[CommandCodes.ToggleSortDirection];
    public IRichCommand SortFoldersFirst => commands[CommandCodes.SortFoldersFirst];
    public IRichCommand SortFilesFirst => commands[CommandCodes.SortFilesFirst];
    public IRichCommand SortFilesAndFoldersTogether => commands[CommandCodes.SortFilesAndFoldersTogether];
    public IRichCommand GroupByNone => commands[CommandCodes.GroupByNone];
	public IRichCommand GroupByName => commands[CommandCodes.GroupByName];
	public IRichCommand GroupByDateModified => commands[CommandCodes.GroupByDateModified];
	public IRichCommand GroupByDateCreated => commands[CommandCodes.GroupByDateCreated];
	public IRichCommand GroupBySize => commands[CommandCodes.GroupBySize];
	public IRichCommand GroupByType => commands[CommandCodes.GroupByType];
	public IRichCommand GroupBySyncStatus => commands[CommandCodes.GroupBySyncStatus];
	public IRichCommand GroupByTag => commands[CommandCodes.GroupByTag];
	public IRichCommand GroupByOriginalFolder => commands[CommandCodes.GroupByOriginalFolder];
	public IRichCommand GroupByDateDeleted => commands[CommandCodes.GroupByDateDeleted];
	public IRichCommand GroupByFolderPath => commands[CommandCodes.GroupByFolderPath];
    public IRichCommand GroupByDateModifiedYear => commands[CommandCodes.GroupByDateModifiedYear];
	public IRichCommand GroupByDateModifiedMonth => commands[CommandCodes.GroupByDateModifiedMonth];
    public IRichCommand GroupByDateModifiedDay => commands[CommandCodes.GroupByDateModifiedDay];
    public IRichCommand GroupByDateCreatedYear => commands[CommandCodes.GroupByDateCreatedYear];
    public IRichCommand GroupByDateCreatedMonth => commands[CommandCodes.GroupByDateCreatedMonth];
    public IRichCommand GroupByDateCreatedDay => commands[CommandCodes.GroupByDateCreatedDay];
    public IRichCommand GroupByDateDeletedYear => commands[CommandCodes.GroupByDateDeletedYear];
    public IRichCommand GroupByDateDeletedMonth => commands[CommandCodes.GroupByDateDeletedMonth];
    public IRichCommand GroupByDateDeletedDay => commands[CommandCodes.GroupByDateDeletedDay];
    public IRichCommand GroupAscending => commands[CommandCodes.GroupAscending];
	public IRichCommand GroupDescending => commands[CommandCodes.GroupDescending];
	public IRichCommand ToggleGroupDirection => commands[CommandCodes.ToggleGroupDirection];
	public IRichCommand GroupByYear => commands[CommandCodes.GroupByYear];
	public IRichCommand GroupByMonth => commands[CommandCodes.GroupByMonth];
	public IRichCommand ToggleGroupByDateUnit => commands[CommandCodes.ToggleGroupByDateUnit];
    public IRichCommand FormatDrive => commands[CommandCodes.FormatDrive];
    public IRichCommand NavigateBack => commands[CommandCodes.NavigateBack];
	public IRichCommand NavigateForward => commands[CommandCodes.NavigateForward];
	public IRichCommand NavigateUp => commands[CommandCodes.NavigateUp];
    public IRichCommand NewWindow => commands[CommandCodes.NewWindow];
    public IRichCommand NewTab => commands[CommandCodes.NewTab];
    // CHANGE: Remove commands related to tabs.
    /*public IRichCommand DuplicateCurrentTab => commands[CommandCodes.DuplicateCurrentTab];
	public IRichCommand DuplicateSelectedTab => commands[CommandCodes.DuplicateSelectedTab];
	public IRichCommand CloseTabsToTheLeftCurrent => commands[CommandCodes.CloseTabsToTheLeftCurrent];
	public IRichCommand CloseTabsToTheLeftSelected => commands[CommandCodes.CloseTabsToTheLeftSelected];
	public IRichCommand CloseTabsToTheRightCurrent => commands[CommandCodes.CloseTabsToTheRightCurrent];
	public IRichCommand CloseTabsToTheRightSelected => commands[CommandCodes.CloseTabsToTheRightSelected];
	public IRichCommand CloseOtherTabsCurrent => commands[CommandCodes.CloseOtherTabsCurrent];
	public IRichCommand CloseOtherTabsSelected => commands[CommandCodes.CloseOtherTabsSelected];*/
    public IRichCommand OpenDirectoryInNewPaneAction => commands[CommandCodes.OpenDirectoryInNewPane];
	public IRichCommand OpenDirectoryInNewTabAction => commands[CommandCodes.OpenDirectoryInNewTab];
	public IRichCommand OpenInNewWindowItemAction => commands[CommandCodes.OpenInNewWindowItem];
    // CHANGE: Remove commands related to tabs.
    /*public IRichCommand ReopenClosedTab => commands[CommandCodes.ReopenClosedTab];
	public IRichCommand PreviousTab => commands[CommandCodes.PreviousTab];
	public IRichCommand NextTab => commands[CommandCodes.NextTab];
	public IRichCommand CloseSelectedTab => commands[CommandCodes.CloseSelectedTab];*/
    public IRichCommand OpenNewPane => commands[CommandCodes.OpenNewPane];
	public IRichCommand ClosePane => commands[CommandCodes.ClosePane];
    public IRichCommand OpenFileLocation => commands[CommandCodes.OpenFileLocation];
	public IRichCommand PlayAll => commands[CommandCodes.PlayAll];
	public IRichCommand GitFetch => commands[CommandCodes.GitFetch];
	public IRichCommand GitInit => commands[CommandCodes.GitInit];
	public IRichCommand GitPull => commands[CommandCodes.GitPull];
	public IRichCommand GitPush => commands[CommandCodes.GitPush];
	public IRichCommand GitSync => commands[CommandCodes.GitSync];
	public IRichCommand OpenAllTaggedItems => commands[CommandCodes.OpenAllTaggedItems];

	public CommandManager()
    {
        /*commands = CreateActions()
                .Select(action => new ActionCommand(this, action.Key, action.Value))
                .Cast<IRichCommand>()
                .Append(new NoneCommand())
                .ToImmutableDictionary(command => command.Code);

        settings.PropertyChanged += Settings_PropertyChanged;
        UpdateHotKeys();*/
    }

    public void Initialize(IFolderViewViewModel folderViewViewModel)
    {
        FolderViewViewModel = folderViewViewModel;
        Settings = FolderViewViewModel.GetService<IGeneralSettingsService>();

        commands = CreateActions(
            FolderViewViewModel, 
            FolderViewViewModel.GetService<IContentPageContext>(),
            FolderViewViewModel.GetService<IDisplayPageContext>())
            .Select(action => new ActionCommand(this, action.Key, action.Value))
            .Cast<IRichCommand>()
            .Append(new NoneCommand())
            .ToImmutableDictionary(command => command.Code);

        Settings.PropertyChanged += Settings_PropertyChanged;
        UpdateHotKeys();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public IEnumerator<IRichCommand> GetEnumerator() => commands.Values.GetEnumerator();

	private static IDictionary<CommandCodes, IAction> CreateActions(IFolderViewViewModel folderViewViewModel, IContentPageContext contentPageContext, IDisplayPageContext displayPageContext) => new Dictionary<CommandCodes, IAction>
	{
        [CommandCodes.OpenHelp] = new OpenHelpAction(),
		[CommandCodes.ToggleFullScreen] = new ToggleFullScreenAction(folderViewViewModel),
		[CommandCodes.EnterCompactOverlay] = new EnterCompactOverlayAction(folderViewViewModel),
		[CommandCodes.ExitCompactOverlay] = new ExitCompactOverlayAction(folderViewViewModel),
		[CommandCodes.ToggleCompactOverlay] = new ToggleCompactOverlayAction(folderViewViewModel),
		[CommandCodes.Search] = new SearchAction(contentPageContext),
		[CommandCodes.EditPath] = new EditPathAction(contentPageContext),
		[CommandCodes.Redo] = new RedoAction(contentPageContext),
		[CommandCodes.Undo] = new UndoAction(contentPageContext),
		[CommandCodes.ToggleShowHiddenItems] = new ToggleShowHiddenItemsAction(folderViewViewModel),
		[CommandCodes.ToggleShowFileExtensions] = new ToggleShowFileExtensionsAction(folderViewViewModel),
		[CommandCodes.TogglePreviewPane] = new TogglePreviewPaneAction(folderViewViewModel),
		[CommandCodes.ToggleDetailsPane] = new ToggleDetailsPaneAction(folderViewViewModel),
		[CommandCodes.ToggleInfoPane] = new ToggleInfoPaneAction(folderViewViewModel),
		[CommandCodes.SelectAll] = new SelectAllAction(contentPageContext),
		[CommandCodes.InvertSelection] = new InvertSelectionAction(contentPageContext),
		[CommandCodes.ClearSelection] = new ClearSelectionAction(contentPageContext),
		[CommandCodes.ToggleSelect] = new ToggleSelectAction(folderViewViewModel),
		[CommandCodes.ShareItem] = new ShareItemAction(folderViewViewModel, contentPageContext),
		[CommandCodes.EmptyRecycleBin] = new EmptyRecycleBinAction(folderViewViewModel, contentPageContext),
		[CommandCodes.RestoreRecycleBin] = new RestoreRecycleBinAction(folderViewViewModel, contentPageContext),
		[CommandCodes.RestoreAllRecycleBin] = new RestoreAllRecycleBinAction(folderViewViewModel),
		[CommandCodes.RefreshItems] = new RefreshItemsAction(contentPageContext),
		[CommandCodes.Rename] = new RenameAction(contentPageContext),
		[CommandCodes.CreateShortcut] = new CreateShortcutAction(folderViewViewModel, contentPageContext),
		[CommandCodes.CreateShortcutFromDialog] = new CreateShortcutFromDialogAction(folderViewViewModel, contentPageContext),
		[CommandCodes.CreateFolder] = new CreateFolderAction(folderViewViewModel, contentPageContext),
		[CommandCodes.CreateFolderWithSelection] = new CreateFolderWithSelectionAction(folderViewViewModel, contentPageContext),
		[CommandCodes.AddItem] = new AddItemAction(folderViewViewModel, contentPageContext),
		[CommandCodes.PinToStart] = new PinToStartAction(folderViewViewModel, contentPageContext),
		[CommandCodes.UnpinFromStart] = new UnpinFromStartAction(contentPageContext),
		[CommandCodes.PinItemToFavorites] = new PinItemAction(folderViewViewModel, contentPageContext),
		[CommandCodes.UnpinItemFromFavorites] = new UnpinItemAction(folderViewViewModel, contentPageContext),
		[CommandCodes.SetAsWallpaperBackground] = new SetAsWallpaperBackgroundAction(folderViewViewModel, contentPageContext),
		[CommandCodes.SetAsSlideshowBackground] = new SetAsSlideshowBackgroundAction(folderViewViewModel, contentPageContext),
		[CommandCodes.SetAsLockscreenBackground] = new SetAsLockscreenBackgroundAction(folderViewViewModel, contentPageContext),
		[CommandCodes.CopyItem] = new CopyItemAction(folderViewViewModel, contentPageContext),
		[CommandCodes.CopyPath] = new CopyPathAction(contentPageContext),
		[CommandCodes.CopyPathWithQuotes] = new CopyPathWithQuotesAction(contentPageContext),
		[CommandCodes.CutItem] = new CutItemAction(folderViewViewModel, contentPageContext),
		[CommandCodes.PasteItem] = new PasteItemAction(contentPageContext),
		[CommandCodes.PasteItemToSelection] = new PasteItemToSelectionAction(folderViewViewModel, contentPageContext),
		[CommandCodes.DeleteItem] = new DeleteItemAction(folderViewViewModel, contentPageContext),
		[CommandCodes.DeleteItemPermanently] = new DeleteItemPermanentlyAction(folderViewViewModel, contentPageContext),
        [CommandCodes.InstallFont] = new InstallFontAction(contentPageContext),
		[CommandCodes.InstallInfDriver] = new InstallInfDriverAction(contentPageContext),
		[CommandCodes.InstallCertificate] = new InstallCertificateAction(contentPageContext),
		[CommandCodes.RunAsAdmin] = new RunAsAdminAction(contentPageContext),
		[CommandCodes.RunAsAnotherUser] = new RunAsAnotherUserAction(contentPageContext),
		[CommandCodes.RunWithPowershell] = new RunWithPowershellAction(contentPageContext),
		[CommandCodes.LaunchPreviewPopup] = new LaunchPreviewPopupAction(contentPageContext),
		[CommandCodes.CompressIntoArchive] = new CompressIntoArchiveAction(folderViewViewModel, contentPageContext),
		[CommandCodes.CompressIntoSevenZip] = new CompressIntoSevenZipAction(folderViewViewModel, contentPageContext),
		[CommandCodes.CompressIntoZip] = new CompressIntoZipAction(folderViewViewModel, contentPageContext),
		[CommandCodes.DecompressArchive] = new DecompressArchive(folderViewViewModel, contentPageContext),
		[CommandCodes.DecompressArchiveHere] = new DecompressArchiveHere(folderViewViewModel, contentPageContext),
		[CommandCodes.DecompressArchiveHereSmart] = new DecompressArchiveHereSmart(folderViewViewModel, contentPageContext),
		[CommandCodes.DecompressArchiveToChildFolder] = new DecompressArchiveToChildFolderAction(folderViewViewModel, contentPageContext),
		[CommandCodes.RotateLeft] = new RotateLeftAction(folderViewViewModel, contentPageContext),
		[CommandCodes.RotateRight] = new RotateRightAction(folderViewViewModel, contentPageContext),
        [CommandCodes.OpenItem] = new OpenItemAction(folderViewViewModel, contentPageContext),
		[CommandCodes.OpenItemWithApplicationPicker] = new OpenItemWithApplicationPickerAction(folderViewViewModel),
		[CommandCodes.OpenParentFolder] = new OpenParentFolderAction(folderViewViewModel),
        [CommandCodes.OpenInVSCode] = new OpenInVSCodeAction(contentPageContext),
		[CommandCodes.OpenRepoInVSCode] = new OpenRepoInVSCodeAction(contentPageContext),
		[CommandCodes.OpenProperties] = new OpenPropertiesAction(folderViewViewModel, contentPageContext),
		[CommandCodes.OpenSettings] = new OpenSettingsAction(folderViewViewModel),
		[CommandCodes.OpenTerminal] = new OpenTerminalAction(contentPageContext),
		[CommandCodes.OpenTerminalAsAdmin] = new OpenTerminalAsAdminAction(contentPageContext),
		[CommandCodes.OpenCommandPalette] = new OpenCommandPaletteAction(contentPageContext),
        [CommandCodes.LayoutDecreaseSize] = new LayoutDecreaseSizeAction(displayPageContext),
		[CommandCodes.LayoutIncreaseSize] = new LayoutIncreaseSizeAction(displayPageContext),
		[CommandCodes.LayoutDetails] = new LayoutDetailsAction(displayPageContext),
        [CommandCodes.LayoutList] = new LayoutListAction(displayPageContext),
        [CommandCodes.LayoutTiles] = new LayoutTilesAction(displayPageContext),
		[CommandCodes.LayoutGridSmall] = new LayoutGridSmallAction(displayPageContext),
		[CommandCodes.LayoutGridMedium] = new LayoutGridMediumAction(displayPageContext),
		[CommandCodes.LayoutGridLarge] = new LayoutGridLargeAction(displayPageContext),
		[CommandCodes.LayoutColumns] = new LayoutColumnsAction(displayPageContext),
		[CommandCodes.LayoutAdaptive] = new LayoutAdaptiveAction(displayPageContext),
		[CommandCodes.SortByName] = new SortByNameAction(contentPageContext, displayPageContext),
		[CommandCodes.SortByDateModified] = new SortByDateModifiedAction(contentPageContext, displayPageContext),
		[CommandCodes.SortByDateCreated] = new SortByDateCreatedAction(contentPageContext, displayPageContext),
		[CommandCodes.SortBySize] = new SortBySizeAction(contentPageContext, displayPageContext),
		[CommandCodes.SortByType] = new SortByTypeAction(contentPageContext, displayPageContext),
		[CommandCodes.SortBySyncStatus] = new SortBySyncStatusAction(contentPageContext, displayPageContext),
		[CommandCodes.SortByTag] = new SortByTagAction(contentPageContext, displayPageContext),
		[CommandCodes.SortByPath] = new SortByPathAction(contentPageContext, displayPageContext),
		[CommandCodes.SortByOriginalFolder] = new SortByOriginalFolderAction(contentPageContext, displayPageContext),
		[CommandCodes.SortByDateDeleted] = new SortByDateDeletedAction(contentPageContext, displayPageContext),
		[CommandCodes.SortAscending] = new SortAscendingAction(displayPageContext),
		[CommandCodes.SortDescending] = new SortDescendingAction(displayPageContext),
		[CommandCodes.ToggleSortDirection] = new ToggleSortDirectionAction(displayPageContext),
        [CommandCodes.SortFoldersFirst] = new SortFoldersFirstAction(displayPageContext),
        [CommandCodes.SortFilesFirst] = new SortFilesFirstAction(displayPageContext),
        [CommandCodes.SortFilesAndFoldersTogether] = new SortFilesAndFoldersTogetherAction(displayPageContext),
		[CommandCodes.GroupByNone] = new GroupByNoneAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByName] = new GroupByNameAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByDateModified] = new GroupByDateModifiedAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByDateCreated] = new GroupByDateCreatedAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupBySize] = new GroupBySizeAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByType] = new GroupByTypeAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupBySyncStatus] = new GroupBySyncStatusAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByTag] = new GroupByTagAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByOriginalFolder] = new GroupByOriginalFolderAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByDateDeleted] = new GroupByDateDeletedAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByFolderPath] = new GroupByFolderPathAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByDateModifiedYear] = new GroupByDateModifiedYearAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByDateModifiedMonth] = new GroupByDateModifiedMonthAction(contentPageContext, displayPageContext),
        [CommandCodes.GroupByDateModifiedDay] = new GroupByDateModifiedDayAction(contentPageContext, displayPageContext),
        [CommandCodes.GroupByDateCreatedYear] = new GroupByDateCreatedYearAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByDateCreatedMonth] = new GroupByDateCreatedMonthAction(contentPageContext, displayPageContext),
        [CommandCodes.GroupByDateCreatedDay] = new GroupByDateCreatedDayAction(contentPageContext, displayPageContext),
        [CommandCodes.GroupByDateDeletedYear] = new GroupByDateDeletedYearAction(contentPageContext, displayPageContext),
		[CommandCodes.GroupByDateDeletedMonth] = new GroupByDateDeletedMonthAction(contentPageContext, displayPageContext),
        [CommandCodes.GroupByDateDeletedDay] = new GroupByDateDeletedDayAction(contentPageContext, displayPageContext),
        [CommandCodes.GroupAscending] = new GroupAscendingAction(displayPageContext),
		[CommandCodes.GroupDescending] = new GroupDescendingAction(displayPageContext),
		[CommandCodes.ToggleGroupDirection] = new ToggleGroupDirectionAction(displayPageContext),
		[CommandCodes.GroupByYear] = new GroupByYearAction(displayPageContext),
		[CommandCodes.GroupByMonth] = new GroupByMonthAction(displayPageContext),
		[CommandCodes.ToggleGroupByDateUnit] = new ToggleGroupByDateUnitAction(displayPageContext),
        [CommandCodes.FormatDrive] = new FormatDriveAction(folderViewViewModel, contentPageContext),
        [CommandCodes.NavigateBack] = new NavigateBackAction(contentPageContext),
		[CommandCodes.NavigateForward] = new NavigateForwardAction(contentPageContext),
		[CommandCodes.NavigateUp] = new NavigateUpAction(contentPageContext),
        [CommandCodes.NewWindow] = new NewWindowAction(folderViewViewModel),
        [CommandCodes.NewTab] = new NewTabAction(folderViewViewModel),
        // CHANGE: Remove commands related to tabs.
        /*[CommandCodes.DuplicateCurrentTab] = new DuplicateCurrentTabAction(),
		[CommandCodes.DuplicateSelectedTab] = new DuplicateSelectedTabAction(),
		[CommandCodes.CloseTabsToTheLeftCurrent] = new CloseTabsToTheLeftCurrentAction(),
		[CommandCodes.CloseTabsToTheLeftSelected] = new CloseTabsToTheLeftSelectedAction(),
		[CommandCodes.CloseTabsToTheRightCurrent] = new CloseTabsToTheRightCurrentAction(),
		[CommandCodes.CloseTabsToTheRightSelected] = new CloseTabsToTheRightSelectedAction(),
		[CommandCodes.CloseOtherTabsCurrent] = new CloseOtherTabsCurrentAction(),
        [CommandCodes.CloseOtherTabsSelected] = new CloseOtherTabsSelectedAction(),*/
        [CommandCodes.OpenDirectoryInNewPane] = new OpenDirectoryInNewPaneAction(folderViewViewModel, contentPageContext),
		[CommandCodes.OpenDirectoryInNewTab] = new OpenDirectoryInNewTabAction(folderViewViewModel, contentPageContext),
		[CommandCodes.OpenInNewWindowItem] = new OpenInNewWindowItemAction(folderViewViewModel, contentPageContext),
        // CHANGE: Remove commands related to tabs.
        /*[CommandCodes.ReopenClosedTab] = new ReopenClosedTabAction(),
		[CommandCodes.PreviousTab] = new PreviousTabAction(),
		[CommandCodes.NextTab] = new NextTabAction(),
		[CommandCodes.CloseSelectedTab] = new CloseSelectedTabAction(),*/
        [CommandCodes.OpenNewPane] = new OpenNewPaneAction(contentPageContext),
		[CommandCodes.ClosePane] = new ClosePaneAction(contentPageContext),
        [CommandCodes.OpenFileLocation] = new OpenFileLocationAction(folderViewViewModel, contentPageContext),
		[CommandCodes.PlayAll] = new PlayAllAction(folderViewViewModel, contentPageContext),
		[CommandCodes.GitFetch] = new GitFetchAction(contentPageContext),
		[CommandCodes.GitInit] = new GitInitAction(folderViewViewModel, contentPageContext),
		[CommandCodes.GitPull] = new GitPullAction(folderViewViewModel, contentPageContext),
		[CommandCodes.GitPush] = new GitPushAction(folderViewViewModel, contentPageContext),
		[CommandCodes.GitSync] = new GitSyncAction(folderViewViewModel, contentPageContext),
		[CommandCodes.OpenAllTaggedItems] = new OpenAllTaggedActions(folderViewViewModel, contentPageContext),
	};

    private void UpdateHotKeys()
	{
		ISet<HotKey> useds = new HashSet<HotKey>();

		var customs = new Dictionary<CommandCodes, HotKeyCollection>();
		foreach (var custom in Settings.Actions)
		{
			if (Enum.TryParse(custom.Key, true, out CommandCodes code))
			{
				if (code is CommandCodes.None)
                {
                    continue;
                }

                var hotKeys = new HotKeyCollection(HotKeyCollection.Parse(custom.Value).Except(useds));
				customs.Add(code, new(hotKeys));

				foreach (var hotKey in hotKeys)
				{
					useds.Add(hotKey with { IsVisible = true });
					useds.Add(hotKey with { IsVisible = false });
				}
			}
		}

		foreach (var command in commands.Values.OfType<ActionCommand>())
		{
			var isCustom = customs.ContainsKey(command.Code);

			var hotkeys = isCustom
				? customs[command.Code]
				: new HotKeyCollection(GetHotKeys(command.Action).Except(useds));

			command.UpdateHotKeys(isCustom, hotkeys);
		}

		hotKeys = commands.Values
			.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
			.ToImmutableDictionary(item => item.HotKey, item => item.Command);
	}

	private static HotKeyCollection GetHotKeys(IAction action)
		=> new(action.HotKey, action.SecondHotKey, action.ThirdHotKey, action.MediaHotKey);

	private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(IGeneralSettingsService.Actions))
        {
            UpdateHotKeys();
        }
    }

    [DebuggerDisplay("Command {Code}")]
	internal class ActionCommand : ObservableObject, IRichCommand
	{
		public event EventHandler? CanExecuteChanged;

		private readonly CommandManager manager;

		public IAction Action { get; }
		public CommandCodes Code { get; }

		public string Label => Action.Label;
		public string LabelWithHotKey => HotKeyText is null ? Label : $"{Label} ({HotKeyText})";
		public string AutomationName => Label;

		public string Description => Action.Description;

		public RichGlyph Glyph => Action.Glyph;
		public object? Icon { get; }
		public FontIcon? FontIcon { get; }
		public Style? OpacityStyle { get; }

        private bool isCustomHotKeys = false;
		public bool IsCustomHotKeys => isCustomHotKeys;

        public string? HotKeyText
		{
			get
			{
				var text = HotKeys.Label;
				if (string.IsNullOrEmpty(text))
                {
                    return null;
                }

                return text;
			}
		}

		private HotKeyCollection hotKeys;
		public HotKeyCollection HotKeys
		{
			get => hotKeys;
			set
			{
				if (hotKeys == value)
                {
                    return;
                }

                var code = Code.ToString();
				var customs = new Dictionary<string, string>(manager.Settings.Actions);

				if (!customs.ContainsKey(code))
                {
                    customs.Add(code, value.Code);
                }
                else if (value != GetHotKeys(Action))
                {
                    customs[code] = value.Code;
                }
                else
                {
                    customs.Remove(code);
                }

                manager.Settings.Actions = customs;
			}
		}

        public bool IsToggle => Action is IToggleAction;

		public bool IsOn
		{
			get => Action is IToggleAction toggleAction && toggleAction.IsOn;
			set
			{
				if (Action is IToggleAction toggleAction && toggleAction.IsOn != value)
                {
                    Execute(null);
                }
            }
		}

		public bool IsExecutable => Action.IsExecutable;

		public ActionCommand(CommandManager manager, CommandCodes code, IAction action)
		{
			this.manager = manager;
			Code = code;
			Action = action;
			Icon = action.Glyph.ToIcon();
			FontIcon = action.Glyph.ToFontIcon();
			OpacityStyle = action.Glyph.ToOpacityStyle();
			hotKeys = GetHotKeys(action);

			if (action is INotifyPropertyChanging notifyPropertyChanging)
            {
                notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
            }

            if (action is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
            }
        }

		public bool CanExecute(object? parameter) => Action.IsExecutable;
		public async void Execute(object? parameter) => await ExecuteAsync();

		public Task ExecuteAsync()
		{
			if (IsExecutable)
			{
				Analytics.TrackEvent($"Triggered {Code} action");
				return Action.ExecuteAsync();
			}

			return Task.CompletedTask;
		}

		public async void ExecuteTapped(object sender, TappedRoutedEventArgs e) => await ExecuteAsync();

		public void ResetHotKeys()
		{
			if (!IsCustomHotKeys)
            {
                return;
            }

            var customs = new Dictionary<string, string>(manager.Settings.Actions);
			customs.Remove(Code.ToString());
			manager.Settings.Actions = customs;
		}

		internal void UpdateHotKeys(bool isCustom, HotKeyCollection hotKeys)
		{
			SetProperty(ref isCustomHotKeys, isCustom, nameof(IsCustomHotKeys));

			if (SetProperty(ref this.hotKeys, hotKeys, nameof(HotKeys)))
			{
				OnPropertyChanged(nameof(HotKeyText));
				OnPropertyChanged(nameof(LabelWithHotKey));
			}
		}

		private void Action_PropertyChanging(object? sender, PropertyChangingEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IAction.Label):
					OnPropertyChanging(nameof(Label));
					OnPropertyChanging(nameof(LabelWithHotKey));
					OnPropertyChanging(nameof(AutomationName));
					break;
				case nameof(IToggleAction.IsOn) when IsToggle:
					OnPropertyChanging(nameof(IsOn));
					break;
				case nameof(IAction.IsExecutable):
					OnPropertyChanging(nameof(IsExecutable));
					break;
			}
		}
		private void Action_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IAction.Label):
					OnPropertyChanged(nameof(Label));
					OnPropertyChanged(nameof(LabelWithHotKey));
					OnPropertyChanged(nameof(AutomationName));
					break;
				case nameof(IToggleAction.IsOn) when IsToggle:
					OnPropertyChanged(nameof(IsOn));
					break;
				case nameof(IAction.IsExecutable):
					OnPropertyChanged(nameof(IsExecutable));
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					break;
			}
		}
	}
}
