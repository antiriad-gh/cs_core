using System.Buffers;
using Antiriad.Core.Log;

namespace Antiriad.Core.IO;

public delegate int ConnectorSizer(ReadOnlySpan<byte> buffer);

public class ConnectorBuffer
{
  private Memory<byte> buffer = Memory<byte>.Empty;
  private int currentSize;

  public ConnectorBuffer(ConnectorSizer sizer)
  {
    this.Sizer = sizer;
  }

  public ConnectorSizer Sizer { get; set; }

  public void Push(ReadOnlySpan<byte> data)
  {
    if (data.IsEmpty)
      return;

    var oldsize = this.currentSize;
    var size = data.Length;

    if (this.buffer.Length < this.currentSize + size)
    {
      var newbuffer = ArrayPool<byte>.Shared.Rent(oldsize + size);

      if (oldsize > 0)
        this.buffer.CopyTo(newbuffer);

      this.buffer = newbuffer;
    }

    data.CopyTo(this.buffer.Span[oldsize..(oldsize + size)]);
    this.currentSize += size;
  }

  public ReadOnlySpan<byte> Pop()
  {
    var result = ReadOnlySpan<byte>.Empty;

    try
    {
      if (this.Sizer != null && this.currentSize > 0)
      {
        var packetSize = this.Sizer(this.buffer.Span[..this.currentSize]);

        if (packetSize > 0 && packetSize <= this.currentSize)
        {
          result = this.buffer.Span[..packetSize];

          if (packetSize < this.currentSize)
          {
            this.currentSize -= packetSize;
            var newbuffer = ArrayPool<byte>.Shared.Rent(this.currentSize);
            this.buffer[packetSize..(packetSize + this.currentSize)].CopyTo(newbuffer);
            this.buffer = newbuffer;
          }
          else
            this.Reset();
        }
        else if (packetSize == -2)
          this.Reset(); //// Corrupted buffer
      }
    }
    catch (Exception ex)
    {
      Trace.Error($"Error {ex.Message}");
      this.Reset();
    }

    return result;
  }

  public void Reset()
  {
    this.buffer = Array.Empty<byte>();
    this.currentSize = 0;

    if (this.Sizer != null)
      _ = this.Sizer(ReadOnlySpan<byte>.Empty);
  }
}
