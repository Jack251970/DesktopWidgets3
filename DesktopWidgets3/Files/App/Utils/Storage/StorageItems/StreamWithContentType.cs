// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace DesktopWidgets3.Files.App.Utils.Storage;

public class InputStreamWithDisposeCallback : IInputStream
{
    private readonly Stream stream;
    private readonly IInputStream iStream;
    public Action DisposeCallback
    {
        get; set;
    }

    public InputStreamWithDisposeCallback(Stream stream)
    {
        this.stream = stream;
        iStream = stream.AsInputStream();
    }

    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        return iStream.ReadAsync(buffer, count, options);
    }

    public void Dispose()
    {
        iStream.Dispose();
        stream.Dispose();
        DisposeCallback?.Invoke();
    }
}

public class NonSeekableRandomAccessStreamForWrite : IRandomAccessStream
{
    private readonly Stream stream;
    private readonly IOutputStream oStream;
    private readonly IRandomAccessStream imrac;
    private ulong byteSize;
    private bool isWritten;

    public Action DisposeCallback
    {
        get; set;
    }

    public NonSeekableRandomAccessStreamForWrite(Stream stream)
    {
        this.stream = stream;
        oStream = stream.AsOutputStream();
        imrac = new InMemoryRandomAccessStream();
    }

    public IInputStream GetInputStreamAt(ulong position)
    {
        throw new NotSupportedException();
    }

    public IOutputStream GetOutputStreamAt(ulong position)
    {
        if (position != 0)
        {
            throw new NotSupportedException();
        }
        return this;
    }

    public void Seek(ulong position)
    {
        if (position != 0)
        {
            throw new NotSupportedException();
        }
    }

    public IRandomAccessStream CloneStream() => throw new NotSupportedException();

    public bool CanRead => false;

    public bool CanWrite => true;

    public ulong Position => byteSize;

    public ulong Size
    {
        get => byteSize;
        set => throw new NotSupportedException();
    }

    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        throw new NotSupportedException();
    }

    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
    {
        Func<CancellationToken, IProgress<uint>, Task<uint>> taskProvider =
            async (token, progress) =>
            {
                var res = await oStream.WriteAsync(buffer);
                byteSize += res;
                return res;
            };

        return AsyncInfo.Run(taskProvider);
    }

    public IAsyncOperation<bool> FlushAsync()
    {
        if (isWritten)
        {
            return imrac.FlushAsync();
        }

        isWritten = true;

        return AsyncInfo.Run<bool>(async (cancellationToken) =>
        {
            await stream.FlushAsync();
            return true;
        });
    }

    public void Dispose()
    {
        oStream.Dispose();
        stream.Dispose();
        imrac.Dispose();
        DisposeCallback?.Invoke();
    }
}

public class NonSeekableRandomAccessStreamForRead : IRandomAccessStream
{
    private readonly Stream stream;
    private readonly IRandomAccessStream imrac;
    private ulong virtualPosition;
    private ulong readToByte;
    private readonly ulong byteSize;

    public Action DisposeCallback
    {
        get; set;
    }

    public NonSeekableRandomAccessStreamForRead(Stream baseStream, ulong size)
    {
        stream = baseStream;
        imrac = new InMemoryRandomAccessStream();
        virtualPosition = 0;
        readToByte = 0;
        byteSize = size;
    }

    public IInputStream GetInputStreamAt(ulong position)
    {
        Seek(position);
        return this;
    }

    public IOutputStream GetOutputStreamAt(ulong position)
    {
        throw new NotSupportedException();
    }

    public void Seek(ulong position)
    {
        imrac.Size = Math.Max(imrac.Size, position);
        virtualPosition = position;
    }

    public IRandomAccessStream CloneStream() => throw new NotSupportedException();

    public bool CanRead => true;

    public bool CanWrite => false;

    public ulong Position => virtualPosition;

    public ulong Size
    {
        get => byteSize;
        set => throw new NotSupportedException();
    }

    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        async Task<IBuffer> taskProvider(CancellationToken token, IProgress<uint> progress)
        {
            int read;
            var tempBuffer = new byte[16384];
            imrac.Seek(readToByte);
            while (imrac.Position < virtualPosition + count)
            {
                read = await stream.ReadAsync(tempBuffer);
                if (read == 0)
                {
                    break;
                }
                await imrac.WriteAsync(tempBuffer.AsBuffer(0, read));
            }
            readToByte = imrac.Position;

            imrac.Seek(virtualPosition);
            var res = await imrac.ReadAsync(buffer, count, options);
            virtualPosition = imrac.Position;
            return res;
        }

        return AsyncInfo.Run((Func<CancellationToken, IProgress<uint>, Task<IBuffer>>)taskProvider);
    }

    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
    {
        throw new NotSupportedException();
    }

    public IAsyncOperation<bool> FlushAsync()
    {
        return imrac.FlushAsync();
    }

    public void Dispose()
    {
        stream.Dispose();
        imrac.Dispose();
        DisposeCallback?.Invoke();
    }
}

public class StreamWithContentType : IRandomAccessStreamWithContentType
{
    private readonly IRandomAccessStream baseStream;

    public StreamWithContentType(IRandomAccessStream stream)
    {
        baseStream = stream;
    }

    public IInputStream GetInputStreamAt(ulong position) => baseStream.GetInputStreamAt(position);

    public IOutputStream GetOutputStreamAt(ulong position) => baseStream.GetOutputStreamAt(position);

    public void Seek(ulong position) => baseStream.Seek(position);

    public IRandomAccessStream CloneStream() => baseStream.CloneStream();

    public bool CanRead => baseStream.CanRead;

    public bool CanWrite => baseStream.CanWrite;

    public ulong Position => baseStream.Position;

    public ulong Size
    {
        get => baseStream.Size; set => baseStream.Size = value;
    }

    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        return baseStream.ReadAsync(buffer, count, options);
    }

    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => baseStream.WriteAsync(buffer);

    public IAsyncOperation<bool> FlushAsync() => baseStream.FlushAsync();

    public void Dispose()
    {
        baseStream.Dispose();
    }

    public string ContentType { get; set; } = "application/octet-stream";
}

public class ComStreamWrapper : Stream
{
    private readonly IStream iStream;
    private STATSTG iStreamStat;

    public ComStreamWrapper(IStream stream)
    {
        iStream = stream;
        iStream.Stat(out iStreamStat, 0);
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => iStreamStat.cbSize;

    public override long Position
    {
        get => Seek(0, SeekOrigin.Current);
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (offset != 0)
        {
            throw new NotSupportedException();
        }

        unsafe
        {
            var newPos = 0;
            iStream.Read(buffer, count, new IntPtr(&newPos));
            return (int)newPos;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        unsafe
        {
            long newPos = 0;
            iStream.Seek(0, (int)origin, new IntPtr(&newPos));
            return newPos;
        }
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Marshal.ReleaseComObject(iStream);
    }
}
