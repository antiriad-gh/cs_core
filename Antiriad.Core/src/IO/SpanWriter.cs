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

  private readonly void Check(int length)
  {
    if (this.Index + length >= this.buffer.Length)
      throw new Exception("index greater than buffer length");
  }

  public void WriteByte(byte value)
  {
    this.Check(1);
    this.buffer[this.Index++] = value;
  }

  public void Write(ReadOnlySpan<byte> value)
  {
    this.Check(value.Length);
    value.CopyTo(this.buffer[this.Index..]);
    this.Index += value.Length;
  }

  public void WriteUShort(ushort value)
  {
    this.Check(2);
    Bytes.FromUShort(value, this.buffer, this.Index, this.le);
    this.Index += 2;
  }

  public void WriteShort(short value)
  {
    this.Check(2);
    Bytes.FromShort(value, this.buffer, this.Index, this.le);
    this.Index += 2;
  }

  public void WriteUInt(uint value)
  {
    this.Check(4);
    Bytes.FromUInt(value, this.buffer, this.Index, this.le);
    this.Index += 4;
  }

  public void WriteInt(int value)
  {
    this.Check(4);
    Bytes.FromInt(value, this.buffer, this.Index, this.le);
    this.Index += 4;
  }

  public void WriteULong(ulong value)
  {
    this.Check(8);
    Bytes.FromULong(value, this.buffer, this.Index, this.le);
    this.Index += 8;
  }

  public void WriteLong(long value)
  {
    this.Check(8);
    Bytes.FromLong(value, this.buffer, this.Index, this.le);
    this.Index += 8;
  }
}