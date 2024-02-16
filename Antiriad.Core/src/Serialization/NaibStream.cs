using System.Collections;
using System.Text;
using Antiriad.Core.Collections;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Serialization;

public class NaibStream
{
  public const int DataTypeMask = 0x3f;
  public const int ArrayMask = 0x40;

  public const int NewObjectMask = 0x8000;
  public const int InfoIdMask = 0x7fff;

  internal static readonly Encoding ByteCoder = Encoding.UTF8;
  internal static readonly byte[] MagicWord = Encoding.ASCII.GetBytes("naib");
  internal const short Version = 1;

  public static int CalculateHash(string value)
  {
    var buffer = ByteCoder.GetBytes(value);
    var hash = 0;

    for (var i = 0; i < buffer.Length; i++)
      hash = 31 * hash + buffer[i];

    return hash;
  }

  public readonly Stream BaseStream;

  public NaibStream() : this(new MemoryStream()) { }

  public NaibStream(Stream baseStream)
  {
    this.BaseStream = baseStream;
  }

  public byte[] GetBytes()
  {
    if (this.BaseStream is MemoryStream s)
    {
      return s.ToArray();
    }

    var buffer = new byte[this.BaseStream.Length];
    this.BaseStream.Read(buffer, 0, buffer.Length);
    return buffer;
  }

  public short ReadVersion()
  {
    var len = NaibStream.MagicWord.Length;
    var magic = this.ReadByteArray(true, len);

    if (ArrayAccessor.ForByte().ElementEquals(magic, NaibStream.MagicWord)) return this.ReadShort();
    if (this.BaseStream.CanSeek && this.BaseStream.Position >= len) this.BaseStream.Seek(-len, SeekOrigin.Current);

    return -1;
  }

  public void WriteVersion()
  {
    this.WriteBuffer(NaibStream.MagicWord); // 4 byte
    this.WriteShort(NaibStream.Version);    // 2 byte
  }

  public byte ReadByte()
  {
    return (byte)this.BaseStream.ReadByte();
  }

  public int ReadSize()
  {
    var size = this.BaseStream.ReadByte();

    return size switch
    {
      127 => this.BaseStream.ReadByte(),
      128 => this.ReadShort(),
      192 => this.ReadInt(),
      _ => size,
    };
  }

  public object ReadByteArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new byte[count];
      this.BaseStream.Read(b, 0, b.Length);
      return b;
    }
    var list = new List<byte>(count);
    for (var i = 0; i < count; i++) list.Add((byte)this.BaseStream.ReadByte());
    return list;
  }

  public object ReadCharArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new byte[count];
      this.BaseStream.Read(b, 0, b.Length);
      return ByteCoder.GetChars(b, 0, b.Length);
    }
    var list = new List<char>(count);
    for (var i = 0; i < count; i++) list.Add((char)this.BaseStream.ReadByte());
    return list;
  }

  public object ReadBoolArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new bool[count];
      for (var i = 0; i < count; i++) b[i] = this.BaseStream.ReadByte() != 0;
      return b;
    }
    var list = new List<bool>(count);
    for (var i = 0; i < count; i++) list.Add(this.BaseStream.ReadByte() != 0);
    return list;
  }

  public object ReadStringArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new string[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadString();
      return b;
    }
    var list = new List<string>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadString());
    return list;
  }

  public object ReadGuidArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new Guid[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadGuid();
      return b;
    }
    var list = new List<Guid>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadGuid());
    return list;
  }

  public object ReadDoubleArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new double[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadDouble();
      return b;
    }
    var list = new List<double>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadDouble());
    return list;
  }

  public object ReadSingleArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new float[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadSingle();
      return b;
    }
    var list = new List<float>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadSingle());
    return list;
  }

  public object ReadDateTimeArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new DateTime[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadDateTime();
      return b;
    }
    var list = new List<DateTime>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadDateTime());
    return list;
  }

  public object ReadBiasedDateTimeArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new DateTimeOffset[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadBiasedDateTime();
      return b;
    }
    var list = new List<DateTimeOffset>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadBiasedDateTime());
    return list;
  }

  public object ReadIntArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new int[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadInt();
      return b;
    }
    var list = new List<int>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadInt());
    return list;
  }

  public object ReadEnumArray(Type etype, int count)
  {
    var enumArray = Array.CreateInstance(etype, count);
    for (var i = 0; i < count; i++) enumArray.SetValue(Enum.ToObject(etype, this.ReadInt()), i);
    return enumArray;
  }

  public object ReadEnumList(Type etype, int count)
  {
    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new[] { etype }), count)!;
    for (var i = 0; i < count; i++) list.Add(Enum.ToObject(etype, this.ReadInt()));
    return list;
  }

  public object ReadUIntArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new uint[count];
      for (var i = 0; i < count; i++) b[i] = (uint)this.ReadInt();
      return b;
    }
    var list = new List<uint>(count);
    for (var i = 0; i < count; i++) list.Add((uint)this.ReadInt());
    return list;
  }

  public object ReadLongArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new long[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadLong();
      return b;
    }
    var list = new List<long>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadLong());
    return list;
  }

  public object ReadULongArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new ulong[count];
      for (var i = 0; i < count; i++) b[i] = (ulong)this.ReadLong();
      return b;
    }
    var list = new List<ulong>(count);
    for (var i = 0; i < count; i++) list.Add((ulong)this.ReadLong());
    return list;
  }

  public object ReadShortArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new short[count];
      for (var i = 0; i < count; i++) b[i] = this.ReadShort();
      return b;
    }
    var list = new List<short>(count);
    for (var i = 0; i < count; i++) list.Add(this.ReadShort());
    return list;
  }

  public object ReadUShortArray(bool createArray, int count)
  {
    if (createArray)
    {
      var b = new ushort[count];
      for (var i = 0; i < count; i++) b[i] = (ushort)this.ReadShort();
      return b;
    }
    var list = new List<ushort>(count);
    for (var i = 0; i < count; i++) list.Add((ushort)this.ReadShort());
    return list;
  }

  public Guid ReadGuid()
  {
    var buffer = new byte[16];
    this.BaseStream.Read(buffer);
    return new Guid(buffer);
  }

  // x=1 y=16 m=4 d=5 h=5 m=6 s=6 m=10 b=1+4+6
  public DateTime ReadDateTime()
  {
    var d = this.ReadLong();
    var year = (int)(d >> 47 & 0xffff);
    var month = (int)(d >> 43 & 0xf);
    var day = (int)(d >> 38 & 0x1f);
    var hour = (int)(d >> 33 & 0x1f);
    var minute = (int)(d >> 27 & 0x3f);
    var second = (int)(d >> 21 & 0x3f);
    var millisecond = (int)(d >> 11 & 0x3ff);
    return new DateTime(year, month, day, hour, minute, second, millisecond);
  }

  public DateTimeOffset ReadBiasedDateTime()
  {
    var d = this.ReadLong();
    var year = (int)(d >> 47 & 0xffff);
    var month = (int)(d >> 43 & 0xf);
    var day = (int)(d >> 38 & 0x1f);
    var hour = (int)(d >> 33 & 0x1f);
    var minute = (int)(d >> 27 & 0x3f);
    var second = (int)(d >> 21 & 0x3f);
    var millisecond = (int)(d >> 11 & 0x3ff);
    var offsetsign = (d & 0x400) > 0 ? -1 : 1;
    var offsethour = (int)(d >> 6 & 0xf) * offsetsign;
    var offsetminute = (int)(d & 0x3f);
    return new DateTimeOffset(year, month, day, hour, minute, second, millisecond, new TimeSpan(offsethour, offsetminute, 0));
  }

  public double ReadDouble()
  {
    var buffer = new byte[8];
    this.BaseStream.Read(buffer, 0, buffer.Length);
    return BitConverter.ToDouble(buffer, 0);
  }

  public string? ReadString(int length)
  {
    if (length <= 0) return null;
    var buffer = new byte[length];
    this.BaseStream.Read(buffer, 0, buffer.Length);
    return ByteCoder.GetString(buffer, 0, buffer.Length);
  }

  public string ReadString()
  {
    return this.ReadString(this.ReadSize());
  }

  public long ReadLong()
  {
    var buffer = new byte[8];
    this.BaseStream.Read(buffer, 0, buffer.Length);
    return Bytes.ToLong(buffer, true);
  }

  public float ReadSingle()
  {
    var buffer = new byte[4];
    this.BaseStream.Read(buffer, 0, buffer.Length);
    return BitConverter.ToSingle(buffer, 0);
  }

  public int ReadInt()
  {
    var buffer = new byte[4];
    this.BaseStream.Read(buffer, 0, buffer.Length);
    return BitConverter.ToInt32(buffer, 0);
  }

  public short ReadShort()
  {
    var buffer = new byte[2];
    this.BaseStream.Read(buffer, 0, buffer.Length);
    return BitConverter.ToInt16(buffer, 0);
  }

  public object? ReadValue(Type type)
  {
    if (type == Typer.TypeByte) return this.ReadByte();
    if (type == Typer.TypeBoolean) return this.ReadByte() != 0;
    if (type == Typer.TypeChar) return (char)this.ReadByte();
    if (type == Typer.TypeShort) return this.ReadShort();
    if (type == Typer.TypeUShort) return (ushort)this.ReadShort();
    if (type == Typer.TypeInt) return this.ReadInt();
    if (type == Typer.TypeUInt) return (uint)this.ReadInt();
    if (type == Typer.TypeFloat) return this.ReadSingle();
    if (type == Typer.TypeLong) return this.ReadLong();
    if (type == Typer.TypeULong) return (ulong)this.ReadLong();
    if (type == Typer.TypeDouble) return this.ReadDouble();
    if (type == Typer.TypeString) return this.ReadString();
    if (type == Typer.TypeDateTime) return this.ReadDateTime();
    if (type == Typer.TypeDateTimeOffset) return this.ReadBiasedDateTime();
    return type == Typer.TypeGuid ? (object)this.ReadGuid() : null;
  }

  public void WriteByte(byte value)
  {
    this.BaseStream.WriteByte(value);
  }

  public void WriteSize(int size)
  {
    if (size < 127)
    {
      this.BaseStream.WriteByte((byte)size);
    }
    else if (size < 256)
    {
      this.BaseStream.WriteByte(127);
      this.BaseStream.WriteByte((byte)size);
    }
    else if (size < short.MaxValue)
    {
      this.BaseStream.WriteByte(128);
      this.WriteShort((short)size);
    }
    else if (size <= int.MaxValue)
    {
      this.BaseStream.WriteByte(192);
      this.WriteInt(size);
    }
  }

  public void WriteGuid(Guid guid)
  {
    var buffer = guid.ToByteArray();
    this.BaseStream.Write(buffer, 0, buffer.Length);
  }

  public void WriteString(string? d)
  {
    if (d == null)
    {
      this.WriteSize(0);
      return;
    }

    var buffer = ByteCoder.GetBytes(d);
    this.WriteSize(buffer.Length);
    this.BaseStream.Write(buffer, 0, buffer.Length);
  }

  public void WriteString(string d, int length)
  {
    var buffer = new byte[length];

    if (d != null)
    {
      var srclen = d.Length;
      ByteCoder.GetBytes(d, 0, length < srclen ? length : srclen, buffer, 0);
    }

    this.WriteSize(length);
    this.BaseStream.Write(buffer, 0, length);
  }

  public void WriteInt(int d)
  {
    var buffer = Bytes.FromInt(d, true);
    this.BaseStream.Write(buffer);
  }

  public void WriteLong(long d)
  {
    var buffer = Bytes.FromLong(d, true);
    this.BaseStream.Write(buffer);
  }

  public void WriteDouble(double d)
  {
    var buffer = BitConverter.GetBytes(d);
    this.BaseStream.Write(buffer);
  }

  public void WriteSingle(float d)
  {
    var buffer = BitConverter.GetBytes(d);
    this.BaseStream.Write(buffer);
  }

  public void WriteShort(short d)
  {
    var buffer = Bytes.FromShort(d, true);
    this.BaseStream.Write(buffer);
  }

  public void WriteDateTime(DateTime value)
  {
    var year = (long)value.Year << 47;
    var month = (long)value.Month << 43;
    var day = (long)value.Day << 38;
    var hour = (long)value.Hour << 33;
    var minute = (long)value.Minute << 27;
    var second = (long)value.Second << 21;
    var millisecond = (long)value.Millisecond << 11;
    this.WriteLong(year | month | day | hour | minute | second | millisecond);
  }

  // x=1 y=16 m=4 d=5 h=5 m=6 s=6 m=10 b=1+4+6
  public void WriteBiasedDateTime(DateTimeOffset value)
  {
    var year = (long)value.Year << 47;
    var month = (long)value.Month << 43;
    var day = (long)value.Day << 38;
    var hour = (long)value.Hour << 33;
    var minute = (long)value.Minute << 27;
    var second = (long)value.Second << 21;
    var millisecond = (long)value.Millisecond << 11;
    var offsetsign = (long)(value.Offset.Hours < 0 ? 1 << 10 : 0);
    var offsethour = (long)Math.Abs(value.Offset.Hours) << 6;
    var offsetminute = (long)value.Offset.Minutes;
    this.WriteLong(year | month | day | hour | minute | second | millisecond | offsetsign | offsethour | offsetminute);
  }

  public void WriteBuffer(ReadOnlySpan<byte> buffer)
  {
    if (buffer == null) return;
    this.BaseStream.Write(buffer);
  }

  public void WriteByteArray(object data)
  {
    if (data is byte[] a)
    {
      this.BaseStream.Write(a, 0, a.Length);
    }
    else if (data is ReadOnlyMemory<byte> ros)
    {
      this.BaseStream.Write(ros.Span);
    }
    else if (data is IEnumerable<byte> e)
    {
      foreach (var i in e) this.BaseStream.WriteByte(i);
    }
  }

  public void WriteCharArray(object data)
  {
    if (data is not char[] a)
    {
      if (data is IEnumerable<char> e)
        foreach (var i in e) this.BaseStream.WriteByte((byte)i);
    }
    else
    {
      var b = ByteCoder.GetBytes(a);
      this.BaseStream.Write(b, 0, b.Length);
    }
  }

  public void WriteBoolArray(object data)
  {
    if (data is not bool[] a)
    {
      if (data is IEnumerable<bool> e)
        foreach (var i in e) this.BaseStream.WriteByte((byte)(i ? 1 : 0));
    }
    else
      for (var i = 0; i < a.Length; i++) this.BaseStream.WriteByte((byte)(a[i] ? 1 : 0));
  }

  public void WriteShortArray(object data)
  {
    if (data is not short[] a)
    {
      if (data is IEnumerable<short> e)
        foreach (var i in e) this.WriteShort(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteShort(a[i]);
  }

  public void WriteUShortArray(object data)
  {
    if (data is not ushort[] a)
    {
      if (data is IEnumerable<ushort> e)
        foreach (var i in e) this.WriteShort((short)i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteShort((short)a[i]);
  }

  public void WriteIntArray(object data)
  {
    if (data is not int[] a)
    {
      if (data is IEnumerable<int> e)
        foreach (var i in e) this.WriteInt(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteInt(a[i]);
  }

  public void WriteEnumArray(object data)
  {
    if (data is IEnumerable e)
      foreach (var i in e) this.WriteInt((int)i);
  }

  public void WriteUIntArray(object data)
  {
    if (data is not uint[] a)
    {
      if (data is IEnumerable<uint> e)
        foreach (var i in e) this.WriteInt((int)i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteInt((int)a[i]);
  }

  public void WriteLongArray(object data)
  {
    if (data is not long[] a)
    {
      if (data is IEnumerable<long> e)
        foreach (var i in e) this.WriteLong(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteLong(a[i]);
  }

  public void WriteULongArray(object data)
  {
    if (data is not ulong[] a)
    {
      if (data is IEnumerable<ulong> e)
        foreach (var i in e) this.WriteLong((long)i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteLong((long)a[i]);
  }

  public void WriteSingleArray(object data)
  {
    if (data is not float[] a)
    {
      if (data is IEnumerable<float> e)
        foreach (var i in e) this.WriteSingle(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteSingle(a[i]);
  }

  public void WriteDoubleArray(object data)
  {
    if (data is not double[] a)
    {
      if (data is IEnumerable<double> e)
        foreach (var i in e) this.WriteDouble(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteDouble(a[i]);
  }

  public void WriteDateTimeArray(object data)
  {
    if (data is not DateTime[] a)
    {
      if (data is IEnumerable<DateTime> e)
        foreach (var i in e) this.WriteDateTime(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteDateTime(a[i]);
  }

  public void WriteStringArray(object data)
  {
    if (data is not string[] a)
    {
      if (data is IEnumerable<string> e)
        foreach (var i in e) this.WriteString(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteString(a[i]);
  }

  public void WriteBiasedDateTimeArray(object data)
  {
    if (data is not DateTimeOffset[] a)
    {
      if (data is IEnumerable<DateTimeOffset> e)
        foreach (var i in e) this.WriteBiasedDateTime(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteBiasedDateTime(a[i]);
  }

  public void WriteGuidArray(object data)
  {
    if (data is not Guid[] a)
    {
      if (data is IEnumerable<Guid> e)
        foreach (var i in e) this.WriteGuid(i);
    }
    else
      for (var i = 0; i < a.Length; i++) this.WriteGuid(a[i]);
  }

  public void WriteValue(Type type, object data)
  {
    if (type == Typer.TypeByte) this.BaseStream.WriteByte(data != null ? (byte)data : (byte)0);
    else if (type == Typer.TypeBoolean) this.BaseStream.WriteByte((byte)(data != null && (bool)data ? 1 : 0));
    else if (type == Typer.TypeChar) this.BaseStream.WriteByte(data != null ? (byte)(char)data : (byte)0);
    else if (type == Typer.TypeShort) this.WriteBuffer(Bytes.FromShort(data != null ? (short)data : (short)0));
    else if (type == Typer.TypeUShort) this.WriteBuffer(Bytes.FromShort(data != null ? (short)(ushort)data : (short)0));
    else if (type == Typer.TypeInt) this.WriteBuffer(Bytes.FromInt(data != null ? (int)data : 0));
    else if (type == Typer.TypeUInt) this.WriteBuffer(Bytes.FromInt(data != null ? (int)(uint)data : 0));
    else if (type == Typer.TypeLong) this.WriteBuffer(Bytes.FromLong(data != null ? (long)data : 0));
    else if (type == Typer.TypeULong) this.WriteBuffer(Bytes.FromLong(data != null ? (long)(ulong)data : 0));
    else if (type == Typer.TypeFloat) this.WriteSingle(data != null ? (float)data : 0);
    else if (type == Typer.TypeDouble) this.WriteDouble(data != null ? (double)data : 0);
    else if (type == Typer.TypeString) this.WriteString(data != null ? (string)data : null);
    else if (type == Typer.TypeDateTime) this.WriteDateTime(data != null ? (DateTime)data : DateTime.MinValue);
    else if (type == Typer.TypeDateTimeOffset) this.WriteBiasedDateTime(data != null ? (DateTimeOffset)data : DateTimeOffset.MinValue);
    else if (type == Typer.TypeGuid) this.WriteGuid(data != null ? (Guid)data : Guid.Empty);
  }
}
