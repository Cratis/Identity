// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ProductTenantsCallback.given;

public class CallbackBodyStream : Stream
{
    public int BytesRead { get; set; }
    public bool Stall { get; set; }
    public bool Broken { get; set; }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (Broken)
        {
            throw new IOException();
        }

        if (Stall)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        buffer.Span.Fill((byte)' ');
        BytesRead += buffer.Length;
        return buffer.Length;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
