namespace Files.App.Utils;

/// <summary>
/// Dictionary manager to support multiple folder view view models of one object.
/// </summary>
public class DictionaryManager<T>(Dictionary<IFolderViewViewModel, T> dictionary, Func<T> create)
{
    private readonly Dictionary<IFolderViewViewModel, T> _dictionary = dictionary;
    private readonly Func<T> _create = create;

    public T Get(IFolderViewViewModel folderViewViewModel)
    {
        if (!_dictionary.TryGetValue(folderViewViewModel, out var value))
        {
            value = _create();
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
