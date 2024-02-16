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

  private readonly void Check(int length)
  {
    if (this.Index + length >= this.buffer.Length)
      throw new Exception("index greater than buffer length");
  }

  public ReadOnlySpan<byte> Read(int length)
  {
    this.Check(length);
    var result = this.buffer[this.Index..(this.Index + length)];
    this.Index += length;
    return result;
  }

  public byte ReadByte()
  {
    this.Check(1);
    return this.buffer[this.Index++];
  }

  public ushort ReadUShort()
  {
    this.Check(2);
    var result = Bytes.ToInt(this.buffer[this.Index..], this.le);
    this.Index += 2;
    return result;
  }

  public short ReadShort()
  {
    this.Check(2);
    var result = Bytes.ToShort(this.buffer[this.Index..], this.le);
    this.Index += 2;
    return result;
  }

  public int ReadInt()
  {
    this.Check(4);
    var result = Bytes.ToInt(this.buffer[this.Index..], this.le);
    this.Index += 4;
    return result;
  }

  public uint ReadUInt()
  {
    this.Check(4);
    var result = Bytes.ToUInt(this.buffer[this.Index..], this.le);
    this.Index += 4;
    return result;
  }

  public long ReadLong()
  {
    this.Check(8);
    var result = Bytes.ToLong(this.buffer[this.Index..], this.le);
    this.Index += 8;
    return result;
  }

  public ulong ReadULong()
  {
    this.Check(8);
    var result = Bytes.ToULong(this.buffer[this.Index..], this.le);
    this.Index += 8;
    return result;
  }
}