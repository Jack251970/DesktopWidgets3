﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Models;

/// <summary>
/// Represents an item for volume info on Windows.
/// </summary>
public readonly struct VolumeInfo : IEquatable<VolumeInfo>
{
	public static VolumeInfo Empty { get; } = new(Guid.Empty);

	public readonly bool IsEmpty
		=> Guid == Guid.Empty;

	public readonly string Id
		=> $@"\\?\Volume{{{Guid}}}";

	public Guid Guid { get; }

	public VolumeInfo(Guid guid)
	{
		Guid = guid;
	}

	public VolumeInfo(string id)
	{
		Guid = ToGuid(id);
	}

	public static implicit operator string(VolumeInfo info)
	{
		return info.Id;
	}

	public static implicit operator Guid(VolumeInfo info)
	{
		return info.Guid;
	}

	public static bool operator ==(VolumeInfo a, VolumeInfo b)
	{
		return a.Guid == b.Guid;
	}

	public static bool operator !=(VolumeInfo a, VolumeInfo b)
	{
		return a.Guid != b.Guid;
	}

	public override string ToString()
	{
		return Id;
	}

	public readonly override int GetHashCode()
	{
		return Guid.GetHashCode();
	}

	public override bool Equals(object? other)
	{
		return other is VolumeInfo info && Equals(info);
	}

	public readonly bool Equals(VolumeInfo other)
	{
		return other.Guid.Equals(Guid);
	}

	private static Guid ToGuid(string id)
	{
		if (string.IsNullOrEmpty(id) || !id.StartsWith(@"\\?\Volume{"))
        {
            return Guid.Empty;
        }

        var guidLength = Guid.Empty.ToString().Length;

		var guid = id.Substring(@"\\?\Volume{".Length, guidLength);

		return Guid.Parse(guid);
	}
}
