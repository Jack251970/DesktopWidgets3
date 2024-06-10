// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers;

internal sealed class CollectionDebugView<T>(ICollection<T> collection)
{
	private readonly ICollection<T> _collection = collection ?? throw new ArgumentNullException(nameof(collection));

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items
	{
		get
		{
			var items = new T[_collection.Count];
			_collection.CopyTo(items, 0);
			return items;
		}
	}
}