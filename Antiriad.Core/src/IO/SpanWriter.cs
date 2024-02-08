using Antiriad.Core.Helpers;

namespace Antiriad.Core.IO;

public ref struct SpanWriter
{
  private readonly Span<byte> buffer;
  private readonly bool le;

  public int Index;

  public SpanWriter(Span<byte> buffer, bool le = true)
  {
    this.buffer = buffer;
    this.le = le;
  }

  private readonly void Check()
  {
    if (this.Index >= this.buffer.Length)
      throw new Exception("index greater than buffer length");
  }

  public void WriteByte(byte value)
  {
    this.Check();
    this.buffer[this.Index++] = value;
  }

  public void WriteUInt16(ushort value)
  {
    this.Check();
    Bytes.FromUShort(value, this.buffer, this.Index, this.le);
    this.Index += 2;
  }
}