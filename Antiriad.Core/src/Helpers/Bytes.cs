using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
  public static short ToShort(ReadOnlySpan<byte> value, bool le = true)
  {
    return le
      ? (short)(value[0] | (value[1] << 8))
      : (short)(value[1] | (value[0] << 8));
  }

  /// <summary>
  /// Obtains a signed int32 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int ToInt(ReadOnlySpan<byte> value, bool le = true)
  {
    return le
      ? value[0] | (value[1] << 8) | (value[2] << 16) | (value[3] << 24)
      : value[3] | (value[2] << 8) | (value[1] << 16) | (value[0] << 24);
  }

  /// <summary>
  /// Obtains a signed int64 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static long ToLong(ReadOnlySpan<byte> value, bool le = true)
  {
    if (le)
    {
      var i1 = value[0] | (value[1] << 8) | (value[2] << 16) | (value[3] << 24);
      var i2 = value[4] | (value[5] << 8) | (value[6] << 16) | (value[7] << 24);
      return (uint)i1 | ((long)i2 << 32);
    }
    else
    {
      var i1 = value[3] | (value[2] << 8) | (value[1] << 16) | (value[0] << 24);
      var i2 = value[7] | (value[6] << 8) | (value[5] << 16) | (value[4] << 24);
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
  public static ushort ToUShort(ReadOnlySpan<byte> value, bool le = true)
  {
    return (ushort)ToShort(value, le);
  }

  /// <summary>
  /// Obtains an unsigned int32 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static uint ToUInt(ReadOnlySpan<byte> value, bool le = true)
  {
    return (uint)ToInt(value, le);
  }

  /// <summary>
  /// Obtains an unsigned int64 value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ulong ToULong(ReadOnlySpan<byte> value, bool le = true)
  {
    return (ulong)ToLong(value, le);
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
  public static float ToFloat(ReadOnlySpan<byte> value, bool le = true)
  {
    return new FloatConverter { IntegerValue = ToInt(value, le) }.FloatValue;
  }

  /// <summary>
  /// Obtains a double value from byte array
  /// </summary>
  /// <param name="value">Array of bytes</param>
  /// <param name="index">Offset to start reading</param>
  /// <param name="le">LittleEndian (default for Intel)</param>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static double ToDouble(ReadOnlySpan<byte> value, bool le = true)
  {
    return new DoubleConverter { LongValue = ToLong(value, le) }.DoubleValue;
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
  public static object? ToNumber(Type vtype, int arrayLength, ReadOnlySpan<byte> buffer, bool le = true)
  {
    return ToObject(vtype, arrayLength, buffer, le);
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
    public static object? ToObject(Type vtype, int arrayLength, ReadOnlySpan<byte> buffer, bool le = true)
    {
        var aa = ArrayAccessor.For(vtype);
        return !vtype.IsArray ? aa.GetValueFromBytes!(buffer, 0, le) : aa.FromBytes(buffer, 0, arrayLength, le);
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
    FromShort(value, buffer, le);
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
    FromInt(value, buffer, le);
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
    FromLong(value, buffer, le);
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
  public static int FromShort(short value, Span<byte> buffer, bool le = true)
  {
    if (le)
    {
      buffer[0] = (byte)(value & 0xff);
      buffer[1] = (byte)((value >> 8) & 0xff);
    }
    else
    {
      buffer[1] = (byte)(value & 0xff);
      buffer[0] = (byte)((value >> 8) & 0xff);
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
  public static int FromInt(int value, Span<byte> buffer, bool le = true)
  {
    if (le)
    {
      buffer[0] = (byte)(value & 0xff);
      buffer[1] = (byte)((value >> 8) & 0xff);
      buffer[2] = (byte)((value >> 16) & 0xff);
      buffer[3] = (byte)((value >> 24) & 0xff);
    }
    else
    {
      buffer[3] = (byte)(value & 0xff);
      buffer[2] = (byte)((value >> 8) & 0xff);
      buffer[1] = (byte)((value >> 16) & 0xff);
      buffer[0] = (byte)((value >> 24) & 0xff);
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
  public static int FromULong(ulong value, Span<byte> buffer, bool le = true)
  {
    return FromLong((long)value, buffer, le);
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
  public static int FromLong(long value, Span<byte> buffer, bool le = true)
  {
    if (le)
    {
      buffer[0] = (byte)(value & 0xff);
      buffer[1] = (byte)((value >> 8) & 0xff);
      buffer[2] = (byte)((value >> 16) & 0xff);
      buffer[3] = (byte)((value >> 24) & 0xff);
      buffer[4] = (byte)((value >> 32) & 0xff);
      buffer[5] = (byte)((value >> 40) & 0xff);
      buffer[6] = (byte)((value >> 48) & 0xff);
      buffer[7] = (byte)((value >> 56) & 0xff);
    }
    else
    {
      buffer[7] = (byte)(value & 0xff);
      buffer[6] = (byte)((value >> 8) & 0xff);
      buffer[5] = (byte)((value >> 16) & 0xff);
      buffer[4] = (byte)((value >> 24) & 0xff);
      buffer[3] = (byte)((value >> 32) & 0xff);
      buffer[2] = (byte)((value >> 40) & 0xff);
      buffer[1] = (byte)((value >> 48) & 0xff);
      buffer[0] = (byte)((value >> 56) & 0xff);
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
  public static int FromUShort(ushort value, Span<byte> buffer, bool le = true)
  {
    return FromShort((short)value, buffer, le);
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
  public static int FromUInt(uint value, Span<byte> buffer, bool le = true)
  {
    return FromInt((int)value, buffer, le);
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
    FromFloat(value, buffer, le);
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
  public static int FromFloat(float value, Span<byte> buffer, bool le = true)
  {
    return FromInt(new FloatConverter { FloatValue = value }.IntegerValue, buffer, le);
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
    FromDouble(value, buffer, le);
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
  public static int FromDouble(double value, Span<byte> buffer, bool le = true)
  {
    return FromLong(new DoubleConverter { DoubleValue = value }.LongValue, buffer, le);
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
        if (vtype.IsArray)
            return aa.ToBytes((Array)value, le);
        var dst = new byte[aa.ElementSize];
        aa.GetBytesFromValue!(Typer.Cast(vtype, value)!, dst, 0, le);
        return dst;
    }

    public static int FromObject(object value, Span<byte> buffer, bool le = true)
    {
        var vtype = value.GetType();
        var aa = ArrayAccessor.For(vtype);
        return vtype.IsArray ? aa.ToBytes((Array)value, 0, aa.GetLength(value), buffer, 0, le) : aa.GetBytesFromValue!(Typer.Cast(vtype, value)!, buffer, 0, le);
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
    public static int FromObject(Type vtype, object value, Span<byte> buffer, bool le = true)
    {
        var aa = ArrayAccessor.For(vtype);
        return vtype.IsArray ? aa.ToBytes((Array)value, 0, aa.GetLength(value), buffer, 0, le) : aa.GetBytesFromValue!(Typer.Cast(vtype, value)!, buffer, 0, le);
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
        Span<char> chars = stackalloc char[checked(value.Length * 2)];

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
    /// <param name="encoding">defaults to UTF8</param>
    /// <returns>Byte array</returns>
    public static ReadOnlySpan<byte> FromHex(string value, Encoding? encoding)
    {
        return FromHex((encoding ?? Encoding.UTF8).GetBytes(value));
    }

    public static int HexToInt(ReadOnlySpan<byte> value)
    {
        return ToInt(FromHex(value), false);
    }

    public static uint HexToUInt(ReadOnlySpan<byte> value)
    {
        return ToUInt(FromHex(value), false);
    }

    public static short HexToShort(ReadOnlySpan<byte> value)
    {
        return ToShort(FromHex(value), false);
    }

    public static ushort HexToUShort(ReadOnlySpan<byte> value)
    {
        return ToUShort(FromHex(value), false);
    }

    public static long HexToLong(ReadOnlySpan<byte> value)
    {
        return ToLong(FromHex(value), false);
    }

    public static ulong HexToULong(ReadOnlySpan<byte> value)
    {
        return ToULong(FromHex(value), false);
    }

  public static ReadOnlySpan<byte> FromHex(ReadOnlySpan<byte> value)
  {
    if (value == null || value.IsEmpty)
      return Array.Empty<byte>();

    unchecked
    {
      var result = new byte[value.Length / 2];

      for (var i = 0; i < result.Length; i++)
      {
        int b = value[i * 2]; // High 4 bits

        if (b > 'Z')
          b -= 32;

        var val = (b - '0' + ((('9' - b) >> 31) & -7)) << 4;

        b = value[i * 2 + 1]; // Low 4 bits

        if (b > 'Z')
          b -= 32;

        val += b - '0' + ((('9' - b) >> 31) & -7);
        result[i] = checked((byte)val);
      }

      return result;
    }
  }
}
