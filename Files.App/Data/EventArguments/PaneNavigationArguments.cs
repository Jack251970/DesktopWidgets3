// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments;

internal class PaneNavigationArguments
{
    public required IFolderViewViewModel FolderViewViewModel { get; set; }

    public string? LeftPaneNavPathParam { get; set; }

	public string? LeftPaneSelectItemParam { get; set; }

	public string? RightPaneNavPathParam { get; set; }

	public string? RightPaneSelectItemParam { get; set; }

	public static bool operator ==(PaneNavigationArguments? a1, PaneNavigationArguments? a2)
	{
		if (a1 is null && a2 is null)
        {
            return true;
        }

        if (a1 is null || a2 is null)
        {
            return false;
        }

        return a1.FolderViewViewModel == a2.FolderViewViewModel &&
            a1.LeftPaneNavPathParam == a2.LeftPaneNavPathParam &&
			a1.LeftPaneSelectItemParam == a2.LeftPaneSelectItemParam &&
			a1.RightPaneNavPathParam == a2.RightPaneNavPathParam &&
            a1.RightPaneSelectItemParam == a2.RightPaneSelectItemParam;
    }

	public static bool operator !=(PaneNavigationArguments? a1, PaneNavigationArguments? a2)
	{
		return !(a1 == a2);
	}

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is PaneNavigationArguments arg)
        {
            return arg == this;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        var hashCode = FolderViewViewModel.GetHashCode();

        if (LeftPaneNavPathParam is not null)
        {
            hashCode = (hashCode * 397) ^ LeftPaneNavPathParam.GetHashCode();
        }
        if (LeftPaneSelectItemParam is not null)
        {
            hashCode = (hashCode * 397) ^ LeftPaneSelectItemParam.GetHashCode();
        }
        if (RightPaneNavPathParam is not null)
        {
            hashCode = (hashCode * 397) ^ RightPaneNavPathParam.GetHashCode();
        }
        if (RightPaneSelectItemParam is not null)
        {
            hashCode = (hashCode * 397) ^ RightPaneSelectItemParam.GetHashCode();
        }

        return hashCode;
    }

    public static PaneNavigationArguments FromJson(IFolderViewViewModel folderViewViewModel, PaneNavigationArgumentsJson args)
    {
        return new PaneNavigationArguments()
        {
            FolderViewViewModel = folderViewViewModel,
            LeftPaneNavPathParam = args.LeftPaneNavPathParam,
            LeftPaneSelectItemParam = args.LeftPaneSelectItemParam,
            RightPaneNavPathParam = args.RightPaneNavPathParam,
            RightPaneSelectItemParam = args.RightPaneSelectItemParam,
        };
    }

    public static PaneNavigationArgumentsJson ToJson(PaneNavigationArguments args)
    {
        return new PaneNavigationArgumentsJson()
        {
            LeftPaneNavPathParam = args.LeftPaneNavPathParam,
            LeftPaneSelectItemParam = args.LeftPaneSelectItemParam,
            RightPaneNavPathParam = args.RightPaneNavPathParam,
            RightPaneSelectItemParam = args.RightPaneSelectItemParam,
        };
    }
}

internal class PaneNavigationArgumentsJson
{
    public string? LeftPaneNavPathParam { get; set; }

    public string? LeftPaneSelectItemParam { get; set; }

    public string? RightPaneNavPathParam { get; set; }

    public string? RightPaneSelectItemParam { get; set;  }

    public static bool operator ==(PaneNavigationArgumentsJson? a1, PaneNavigationArgumentsJson? a2)
    {
        if (a1 is null && a2 is null)
        {
            return true;
        }

        if (a1 is null || a2 is null)
        {
            return false;
        }

        return a1.LeftPaneNavPathParam == a2.LeftPaneNavPathParam &&
            a1.LeftPaneSelectItemParam == a2.LeftPaneSelectItemParam &&
            a1.RightPaneNavPathParam == a2.RightPaneNavPathParam &&
            a1.RightPaneSelectItemParam == a2.RightPaneSelectItemParam;
    }

    public static bool operator !=(PaneNavigationArgumentsJson? a1, PaneNavigationArgumentsJson? a2)
    {
        return !(a1 == a2);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is PaneNavigationArgumentsJson arg)
        {
            return arg == this;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        var hashCode = GetHashCode();

        if (LeftPaneNavPathParam is not null)
        {
            hashCode = (hashCode * 397) ^ LeftPaneNavPathParam.GetHashCode();
        }
        if (LeftPaneSelectItemParam is not null)
        {
            hashCode = (hashCode * 397) ^ LeftPaneSelectItemParam.GetHashCode();
        }
        if (RightPaneNavPathParam is not null)
        {
            hashCode = (hashCode * 397) ^ RightPaneNavPathParam.GetHashCode();
        }
        if (RightPaneSelectItemParam is not null)
        {
            hashCode = (hashCode * 397) ^ RightPaneSelectItemParam.GetHashCode();
        }

        return hashCode;
    }
}
