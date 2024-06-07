namespace Files.App.Data.EventArguments;

internal sealed class PanePathNavigationArguments
{
    public required IFolderViewViewModel FolderViewViewModel { get; set; }

    public string? NavPathParam { get; set; }

    public static bool operator ==(PanePathNavigationArguments? a1, PanePathNavigationArguments? a2)
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
            a1.NavPathParam == a2.NavPathParam;
    }

    public static bool operator !=(PanePathNavigationArguments? a1, PanePathNavigationArguments? a2)
    {
        return !(a1 == a2);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is PanePathNavigationArguments arg)
        {
            return arg == this;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        var hashCode = FolderViewViewModel.GetHashCode();

        if (NavPathParam is not null)
        {
            hashCode = (hashCode * 397) ^ NavPathParam.GetHashCode();
        }

        return hashCode;
    }
}
