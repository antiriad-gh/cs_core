using Antiriad.Core.Helpers;

namespace Antiriad.Core.IO;

public ref struct SpanReader
{
  private readonly ReadOnlySpan<byte> buffer;
  private readonly bool le;

  public int Index;

  public SpanReader(ReadOnlySpan<byte> buffer, bool le = true)
  {
    this.buffer = buffer;
    this.le = le;
  }

  private readonly void Check()
  {
    if (this.Index >= this.buffer.Length)
      throw new Exception("index greater than buffer length");
  }

  public byte ReadByte()
  {
    this.Check();
    return this.buffer[this.Index++];
  }

  public int ReadUInt16()
  {
    this.Check();
    var result = Bytes.ToInt(this.buffer, this.Index, this.le);
    this.Index += 2;
    return result;
  }
}