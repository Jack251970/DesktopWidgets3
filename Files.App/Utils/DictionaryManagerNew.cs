namespace Files.App.Utils;

/// <summary>
/// Dictionary manager to support multiple folder view view models of one object.
/// Initialize unexisting values with new T().
/// </summary>
public class DictionaryManagerNew<T>(Dictionary<IFolderViewViewModel, T> dictionary) 
    where T : new()
{
    private readonly Dictionary<IFolderViewViewModel, T> _dictionary = dictionary;

    public T Get(IFolderViewViewModel folderViewViewModel)
    {
        if (!_dictionary.TryGetValue(folderViewViewModel, out var value))
        {
            value = new T();
            _dictionary[folderViewViewModel] = value;
        }
        return value;
    }

    public void Set(IFolderViewViewModel folderViewViewModel, T value)
    {
        if (!_dictionary.TryAdd(folderViewViewModel, value))
        {
            _dictionary[folderViewViewModel] = value;
        }
    }

    public void Remove(IFolderViewViewModel folderViewViewModel)
    {
        _dictionary.Remove(folderViewViewModel);
    }
}

/// <summary>
/// Dictionary manager to support multiple folder view view models of one object.
/// Initialize unexisting values with default.
/// </summary>
public class DictionaryManagerDefault<T>(Dictionary<IFolderViewViewModel, T?> dictionary)
{
    private readonly Dictionary<IFolderViewViewModel, T?> _dictionary = dictionary;

    public T? Get(IFolderViewViewModel folderViewViewModel)
    {
        if (!_dictionary.TryGetValue(folderViewViewModel, out var value))
        {
            _dictionary[folderViewViewModel] = value;
        }
        return value;
    }

    public void Set(IFolderViewViewModel folderViewViewModel, T value)
    {
        if (!_dictionary.TryAdd(folderViewViewModel, value))
        {
            _dictionary[folderViewViewModel] = value;
        }
    }

    public void Remove(IFolderViewViewModel folderViewViewModel)
    {
        _dictionary.Remove(folderViewViewModel);
    }
}

