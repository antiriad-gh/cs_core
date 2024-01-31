using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Antiriad.Core.Collections;

namespace Antiriad.Core.Helpers;

/// <summary>
/// Conversion from basic types to bytes array and back
/// </summary>
public static class Bytes
{
  /// <summary>
  /// Obtains a signed int16 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static short ToShort(ReadOnlySpan<byte> value, int index = 0, bool le = true)
  {
    return le
      ? (short)(value[index + 0] | (value[index + 1] << 8))
      : (short)(value[index + 1] | (value[index + 0] << 8));
  }

  /// <summary>
  /// Obtains a signed int32 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int ToInt(ReadOnlySpan<byte> value, int index = 0, bool le = true)
  {
    return le
      ? value[index + 0] | (value[index + 1] << 8) | (value[index + 2] << 16) | (value[index + 3] << 24)
      : value[index + 3] | (value[index + 2] << 8) | (value[index + 1] << 16) | (value[index + 0] << 24);
  }

  /// <summary>
  /// Obtains a signed int64 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static long ToLong(ReadOnlySpan<byte> value, int index = 0, bool le = true)
  {
    if (le)
    {
      var i1 = value[index + 0] | (value[index + 1] << 8) | (value[index + 2] << 16) | (value[index + 3] << 24);
      var i2 = value[index + 4] | (value[index + 5] << 8) | (value[index + 6] << 16) | (value[index + 7] << 24);
      return (uint)i1 | ((long)i2 << 32);
    }
    else
    {
      var i1 = value[index + 3] | (value[index + 2] << 8) | (value[index + 1] << 16) | (value[index + 0] << 24);
      var i2 = value[index + 7] | (value[index + 6] << 8) | (value[index + 5] << 16) | (value[index + 4] << 24);
      return (uint)i1 | ((long)i2 << 32);
    }
  }

  /// <summary>
  /// Obtains an unsigned int16 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ushort ToUShort(ReadOnlySpan<byte> value, int index = 0, bool le = true)
  {
    return (ushort)ToShort(value, index, le);
  }

  /// <summary>
  /// Obtains an unsigned int32 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static uint ToUInt(ReadOnlySpan<byte> value, int index = 0, bool le = true)
  {
    return (uint)ToInt(value, index, le);
  }

  /// <summary>
  /// Obtains an unsigned int64 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ulong ToULong(ReadOnlySpan<byte> value, int index = 0, bool le = true)
  {
    return (ulong)ToLong(value, index, le);
  }

  [StructLayout(LayoutKind.Explicit)]
  private struct FloatConverter
  {
    [FieldOffset(0)]
    public float FloatValue;

    [FieldOffset(0)]
    public int IntegerValue;
  }

  [StructLayout(LayoutKind.Explicit)]
  private struct DoubleConverter
  {
    [FieldOffset(0)]
    public double DoubleValue;

    [FieldOffset(0)]
    public long LongValue;
  }

  /// <summary>
  /// Obtains a float value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float ToFloat(ReadOnlySpan<byte> value, int index, bool le = true)
  {
    return new FloatConverter { IntegerValue = ToInt(value, index, le) }.FloatValue;
  }

  /// <summary>
  /// Obtains a double value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static double ToDouble(ReadOnlySpan<byte> value, int index, bool le = true)
  {
    return new DoubleConverter { LongValue = ToLong(value, index, le) }.DoubleValue;
  }

  /// <summary>
  /// Obtains a <code>vtype</code> value from byte array
  /// </summary>
  /// <param name="vtype">Type for desired return value</param>
  /// <param name="arrayLength">Array length</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [Obsolete("use ToObject()")]
  public static object? ToNumber(Type vtype, int arrayLength, ReadOnlySpan<byte> buffer, int index = 0, bool le = true)
  {
    return ToObject(vtype, arrayLength, buffer, index, le);
  }

  /// <summary>
  /// Obtains a <code>vtype</code> value from byte array
  /// </summary>
  /// <param name="vtype">Type for desired return value</param>
  /// <param name="arrayLength">Array length</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  public static object? ToObject(Type vtype, int arrayLength, ReadOnlySpan<byte> buffer, int index = 0, bool le = true)
  {
    var aa = ArrayAccessor.For(vtype);
    if (!vtype.IsArray) return aa.GetValueFromBytes!(buffer, index, le);
    return aa.FromBytes(buffer, index, arrayLength, le);
  }

  /// <summary>
  /// Obtains a byte array from unsigned int16 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte[] FromUShort(ushort value, bool le = true)
  {
    return FromShort((short)value, le);
  }

  /// <summary>
  /// Obtains a byte array from int16 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte[] FromShort(short value, bool le = true)
  {
    var buffer = new byte[2];
    FromShort(value, buffer, 0, le);
    return buffer;
  }

  /// <summary>
  /// Obtains a byte array from unsigned int32 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte[] FromUInt(uint value, bool le = true)
  {
    return FromInt((int)value, le);
  }

  /// <summary>
  /// Obtains a byte array from int32 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte[] FromInt(int value, bool le = true)
  {
    var buffer = new byte[4];
    FromInt(value, buffer, 0, le);
    return buffer;
  }

  /// <summary>
  /// Obtains a byte array from unsigned int64 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte[] FromULong(ulong value, bool le = true)
  {
    return FromLong((long)value, le);
  }

  /// <summary>
  /// Obtains a byte array from int64 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte[] FromLong(long value, bool le = true)
  {
    var buffer = new byte[8];
    FromLong(value, buffer, 0, le);
    return buffer;
  }

  /// <summary>
  /// Obtains a byte array from int16 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start writing</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int FromShort(short value, Span<byte> buffer, int index = 0, bool le = true)
  {
    if (le)
    {
      buffer[index + 0] = (byte)(value & 0xff);
      buffer[index + 1] = (byte)((value >> 8) & 0xff);
    }
    else
    {
      buffer[index + 1] = (byte)(value & 0xff);
      buffer[index + 0] = (byte)((value >> 8) & 0xff);
    }
    return 2;
  }

  /// <summary>
  /// Obtains a byte array from int32 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start writing</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int FromInt(int value, Span<byte> buffer, int index = 0, bool le = true)
  {
    if (le)
    {
      buffer[index + 0] = (byte)(value & 0xff);
      buffer[index + 1] = (byte)((value >> 8) & 0xff);
      buffer[index + 2] = (byte)((value >> 16) & 0xff);
      buffer[index + 3] = (byte)((value >> 24) & 0xff);
    }
    else
    {
      buffer[index + 3] = (byte)(value & 0xff);
      buffer[index + 2] = (byte)((value >> 8) & 0xff);
      buffer[index + 1] = (byte)((value >> 16) & 0xff);
      buffer[index + 0] = (byte)((value >> 24) & 0xff);
    }
    return 4;
  }

  /// <summary>
  /// Obtains a byte array from unsigned int64 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start writing</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int FromULong(ulong value, Span<byte> buffer, int index = 0, bool le = true)
  {
    return FromLong((long)value, buffer, index, le);
  }

  /// <summary>
  /// Obtains a byte array from int64 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start writing</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int FromLong(long value, Span<byte> buffer, int index = 0, bool le = true)
  {
    if (le)
    {
      buffer[index + 0] = (byte)(value & 0xff);
      buffer[index + 1] = (byte)((value >> 8) & 0xff);
      buffer[index + 2] = (byte)((value >> 16) & 0xff);
      buffer[index + 3] = (byte)((value >> 24) & 0xff);
      buffer[index + 4] = (byte)((value >> 32) & 0xff);
      buffer[index + 5] = (byte)((value >> 40) & 0xff);
      buffer[index + 6] = (byte)((value >> 48) & 0xff);
      buffer[index + 7] = (byte)((value >> 56) & 0xff);
    }
    else
    {
      buffer[index + 7] = (byte)(value & 0xff);
      buffer[index + 6] = (byte)((value >> 8) & 0xff);
      buffer[index + 5] = (byte)((value >> 16) & 0xff);
      buffer[index + 4] = (byte)((value >> 24) & 0xff);
      buffer[index + 3] = (byte)((value >> 32) & 0xff);
      buffer[index + 2] = (byte)((value >> 40) & 0xff);
      buffer[index + 1] = (byte)((value >> 48) & 0xff);
      buffer[index + 0] = (byte)((value >> 56) & 0xff);
    }
    return 8;
  }

  /// <summary>
  /// Obtains a byte array from unsigned int16 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start writing</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int FromUShort(ushort value, Span<byte> buffer, int index = 0, bool le = true)
  {
    return FromShort((short)value, buffer, index, le);
  }

  /// <summary>
  /// Obtains a byte array from unsigned int32 value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start writing</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int FromUInt(uint value, Span<byte> buffer, int index = 0, bool le = true)
  {
    return FromInt((int)value, buffer, index, le);
  }

  /// <summary>
  /// Obtains a byte array from float value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  public static byte[] FromFloat(float value, bool le = true)
  {
    var buffer = new byte[4];
    FromFloat(value, buffer, 0, le);
    return buffer;
  }

  /// <summary>
  /// Obtains a byte array from float value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start writing</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  public static int FromFloat(float value, byte[] buffer, int index, bool le = true)
  {
    return FromInt(new FloatConverter { FloatValue = value }.IntegerValue, buffer, index, le);
  }

  /// <summary>
  /// Obtains a byte array from double value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  public static byte[] FromDouble(double value, bool le = true)
  {
    var buffer = new byte[8];
    FromDouble(value, buffer, 0, le);
    return buffer;
  }

  /// <summary>
  /// Obtains a byte array from double value
  /// </summary>
  /// <param name="value">The number to convert to byte array</param>
  /// <param name="buffer">Array of bytes</param>
  /// <param name="index">Offset to start writing</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Count of bytes written</returns>
  public static int FromDouble(double value, byte[] buffer, int index, bool le = true)
  {
    return FromLong(new DoubleConverter { DoubleValue = value }.LongValue, buffer, index, le);
  }

  /// <summary>
  /// Copies char Array to byte Array
  /// </summary>
  /// <param name="source">Source Array</param>
  /// <param name="sourceIndex">Source Array offset</param>
  /// <param name="dest">Destination Array</param>
  /// <param name="destIndex">Destination Array offset</param>
  /// <param name="count">Count of bytes to copy</param>
  /// <returns>Count of bytes copied</returns>
  public static int FromChars(char[] source, int sourceIndex, byte[] dest, int destIndex, int count)
  {
    Buffer.BlockCopy(source, sourceIndex, dest, destIndex, count);
    return count;
  }

  /// <summary>
  /// Copies byte Array to char Array
  /// </summary>
  /// <param name="source">Source Array</param>
  /// <param name="sourceIndex">Source Array offset</param>
  /// <param name="dest">Destination Array</param>
  /// <param name="destIndex">Destination Array offset</param>
  /// <param name="count">Count of bytes to copy</param>
  /// <returns>Count of bytes copied</returns>
  public static int FromBytes(byte[] source, int sourceIndex, byte[] dest, int destIndex, int count)
  {
    Buffer.BlockCopy(source, sourceIndex, dest, destIndex, count);
    return count;
  }

  /// <summary>
  /// Get byte size from given Type
  /// </summary>
  /// <param name="vtype">Type of data</param>
  /// <returns>Count of bytes needed to hold the value</returns>
  public static byte GetSize(Type vtype)
  {
    if (vtype == Typer.TypeByte || vtype == Typer.TypeByteArray)
      return 1;

    if (vtype == Typer.TypeUShort || vtype == Typer.TypeShort ||
      vtype == Typer.TypeUShortArray || vtype == Typer.TypeShortArray)
      return 2;

    if (vtype == Typer.TypeUInt || vtype == Typer.TypeInt ||
      vtype == Typer.TypeUIntArray || vtype == Typer.TypeIntArray ||
      vtype == Typer.TypeFloat || vtype == Typer.TypeFloatArray)
      return 4;

    if (vtype == Typer.TypeLong || vtype == Typer.TypeULong ||
      vtype == Typer.TypeDouble || vtype == Typer.TypeDoubleArray)
      return 8;

    return 1;
  }

  /// <summary>
  /// Gets Array of bytes from a number or Array of numbers
  /// </summary>
  /// <param name="value">A number or Array of numbers</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns>Byte array containing the value</returns>
  public static byte[] FromObject(object value, bool le = true)
  {
    var vtype = value.GetType();
    var aa = ArrayAccessor.For(vtype);
    if (vtype.IsArray) return aa.ToBytes((Array)value, le);
    var dst = new byte[aa.ElementSize];
    aa.GetBytesFromValue!(Typer.Cast(vtype, value), dst, 0, le);
    return dst;
  }

  public static int FromObject(object value, Span<byte> buffer, int index = 0, bool le = true)
  {
    var vtype = value.GetType();
    var aa = ArrayAccessor.For(vtype);
    if (vtype.IsArray) return aa.ToBytes((Array)value, 0, aa.GetLength(value), buffer, index, le);
    return aa.GetBytesFromValue!(Typer.Cast(vtype, value), buffer, index, le);
  }

  /// <summary>
  /// Gets Array of bytes from a number or Array of numbers
  /// </summary>
  /// <param name="vtype">Type of data</param>
  /// <param name="value">Value to convert</param>
  /// <param name="buffer">Array of byte to hold the value</param>
  /// <param name="index">Destination Array offset</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  public static int FromObject(Type vtype, object value, Span<byte> buffer, int index = 0, bool le = true)
  {
    var aa = ArrayAccessor.For(vtype);
    if (vtype.IsArray) return aa.ToBytes((Array)value, 0, aa.GetLength(value), buffer, index, le);
    return aa.GetBytesFromValue!(Typer.Cast(vtype, value), buffer, index, le);
  }

  /// <summary>
  /// Receives a byte array and produces an hexadecimal string
  /// </summary>
  /// <param name="value">Byte array value</param>
  /// <returns>String with hexadecimal value</returns>
  public static string ToHex(ReadOnlySpan<byte> value)
  {
    if (value == null)
      return string.Empty;

    const string hexDigits = @"0123456789ABCDEF";
    Span<char> chars = new char[checked(value.Length * 2)];

    unchecked
    {
      for (var i = 0; i < value.Length; i++)
      {
        chars[i * 2] = hexDigits[value[i] >> 4];
        chars[i * 2 + 1] = hexDigits[value[i] & 0xf];
      }
    }
    return new string(chars);
  }

  /// <summary>
  /// Receives string with hexadecimal value and returns byte array
  /// </summary>
  /// <param name="value">String containing hexadecimal</param>
  /// <returns>Byte array</returns>
  public static byte[] FromHex(string value)
  {
    return FromHex(value, 0, value.Length);
  }

  public static byte[] FromHex(string value, int index, int count)
  {
    if (value == null)
      return Array.Empty<byte>();

    unchecked
    {
      var result = new byte[count / 2];

      for (var i = 0; i < result.Length; i++)
      {
        int b = value[index + i * 2]; // High 4 bits

        if (b > 'Z')
          b -= 32;

        var val = (b - '0' + ((('9' - b) >> 31) & -7)) << 4;

        b = value[index + i * 2 + 1]; // Low 4 bits

        if (b > 'Z')
          b -= 32;

        val += b - '0' + ((('9' - b) >> 31) & -7);
        result[i] = checked((byte)val);
      }

      return result;
    }
  }
}
