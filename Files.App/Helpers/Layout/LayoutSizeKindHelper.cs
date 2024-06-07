// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers;

public static class LayoutSizeKindHelper
{
	/*private static ILayoutSettingsService LayoutSettingsService { get; } = DependencyExtensions.GetService<ILayoutSettingsService>();*/

	/// <summary>
	/// Gets the desired height for items in the Details View
	/// </summary>
	/// <param name="detailsViewSizeKind"></param>
	/// <returns></returns>
	public static int GetDetailsViewRowHeight(DetailsViewSizeKind detailsViewSizeKind)
	{
        return detailsViewSizeKind switch
        {
            DetailsViewSizeKind.Compact => 28,
            DetailsViewSizeKind.Small => 36,
            DetailsViewSizeKind.Medium => 40,
            DetailsViewSizeKind.Large => 44,
            DetailsViewSizeKind.ExtraLarge => 48,
            _ => 36,
        };
    }

	/// <summary>
	/// Gets the desired width for items in the Grid View
	/// </summary>
	/// <param name="gridViewSizeKind"></param>
	/// <returns></returns>
	public static int GetGridViewItemWidth(GridViewSizeKind gridViewSizeKind)
	{
        return gridViewSizeKind switch
        {
            GridViewSizeKind.Small => 80,
            GridViewSizeKind.Medium => 100,
            GridViewSizeKind.Three => 120,
            GridViewSizeKind.Four => 140,
            GridViewSizeKind.Five => 160,
            GridViewSizeKind.Six => 180,
            GridViewSizeKind.Seven => 200,
            GridViewSizeKind.Large => 220,
            GridViewSizeKind.Nine => 240,
            GridViewSizeKind.Ten => 260,
            GridViewSizeKind.Eleven => 280,
            GridViewSizeKind.ExtraLarge => 300,
            _ => 100,
        };
    }

	/// <summary>
	/// Gets the desired height for items in the List View
	/// </summary>
	/// <param name="listViewSizeKind"></param>
	/// <returns></returns>
	public static int GetListViewRowHeight(ListViewSizeKind listViewSizeKind)
	{
        return listViewSizeKind switch
        {
            ListViewSizeKind.Compact => 24,
            ListViewSizeKind.Small => 32,
            ListViewSizeKind.Medium => 36,
            ListViewSizeKind.Large => 40,
            ListViewSizeKind.ExtraLarge => 44,
            _ => 32,
        };
    }

	/// <summary>
	/// Gets the desired height for items in the Columns View
	/// </summary>
	/// <param name="columnsViewSizeKind"></param>
	/// <returns></returns>
	public static int GetColumnsViewRowHeight(ColumnsViewSizeKind columnsViewSizeKind)
	{
        return columnsViewSizeKind switch
        {
            ColumnsViewSizeKind.Compact => 24,
            ColumnsViewSizeKind.Small => 32,
            ColumnsViewSizeKind.Medium => 36,
            ColumnsViewSizeKind.Large => 40,
            ColumnsViewSizeKind.ExtraLarge => 44,
            _ => 32,
        };
	}

	/// <summary>
	/// Gets the desired width for items in the Tiles View
	/// </summary>
	/// <param name="tilesViewSizeKind"></param>
	/// <returns></returns>
	public static int GetTilesViewItemWidth(TilesViewSizeKind tilesViewSizeKind)
	{
		return 260;
	}
}