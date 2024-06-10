// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage;

public sealed class BlockingListEnumerator<T>(IList<T> inner, object @lock) : IEnumerator<T>
{
	private readonly IList<T> m_Inner = inner;

	private readonly object m_Lock = @lock;

	private T m_Current;

	private int m_Pos = -1;

	public T Current
	{
		get
		{
			lock (m_Lock)
            {
                return m_Current;
            }
        }
	}

	object IEnumerator.Current
		=> Current!;

    public void Dispose()
	{
	}

	public bool MoveNext()
	{
		lock (m_Lock)
		{
			m_Pos++;
			var hasNext = m_Pos < m_Inner.Count;
			if (hasNext)
			{
				m_Current = m_Inner[m_Pos];
			}
			return hasNext;
		}
	}

	public void Reset()
	{
		lock (m_Lock)
		{
			m_Pos = -1;
		}
	}
}
