// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage;

public sealed class StorageHistory(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination) : IStorageHistory
{
    public FileOperationType OperationType { get; private set; } = operationType;

    public IList<IStorageItemWithPath> Source { get; private set; } = source;

    public IList<IStorageItemWithPath> Destination { get; private set; } = destination;

    public StorageHistory(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
		: this(operationType, source.CreateList(), destination.CreateList())
	{
	}

    public void Modify(IStorageHistory newHistory)
		=> (OperationType, Source, Destination) = (newHistory.OperationType, newHistory.Source, newHistory.Destination);
	public void Modify(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
		=> (OperationType, Source, Destination) = (operationType, source.CreateList(), destination.CreateList());
	public void Modify(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination)
		=> (OperationType, Source, Destination) = (operationType, source, destination);
}