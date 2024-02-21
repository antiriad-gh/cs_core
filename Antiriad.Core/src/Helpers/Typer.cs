using System.Reflection;
using System.Runtime.CompilerServices;
using Antiriad.Core.Collections;
using Antiriad.Core.Types;

namespace Antiriad.Core.Helpers;

/// <summary>
/// Static class for Type conversion
/// </summary>
public static class Typer
{
  ///<summary>
  ///</summary>
  public static readonly Type TypeUnknown = null!;

  ///<summary>
  ///</summary>
  public static readonly Type TypeVoid = typeof(void);

  ///<summary>
  ///</summary>
  public static readonly Type TypeObject = typeof(object);

  ///<summary>
  ///</summary>
  public static readonly Type TypeBoolean = typeof(bool);

  ///<summary>
  ///</summary>
  public static readonly Type TypeByte = typeof(byte);

  ///<summary>
  ///</summary>
  public static readonly Type TypeSByte = typeof(sbyte);

  ///<summary>
  ///</summary>
  public static readonly Type TypeChar = typeof(char);

  ///<summary>
  ///</summary>
  public static readonly Type TypeShort = typeof(short);

  ///<summary>
  ///</summary>
  public static readonly Type TypeInt = typeof(int);

  ///<summary>
  ///</summary>
  public static readonly Type TypeFloat = typeof(float);

  ///<summary>
  ///</summary>
  public static readonly Type TypeDouble = typeof(double);

  ///<summary>
  ///</summary>
  public static readonly Type TypeString = typeof(string);

  ///<summary>
  ///</summary>
  public static readonly Type TypeObjectArray = typeof(object[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeBoolArray = typeof(bool[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeCharArray = typeof(char[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeByteArray = typeof(byte[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeShortArray = typeof(short[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeIntArray = typeof(int[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeFloatArray = typeof(float[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeDoubleArray = typeof(double[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeStringArray = typeof(string[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeUShort = typeof(ushort);

  ///<summary>
  ///</summary>
  public static readonly Type TypeUInt = typeof(uint);

  ///<summary>
  ///</summary>
  public static readonly Type TypeUShortArray = typeof(ushort[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeUIntArray = typeof(uint[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeLong = typeof(long);

  ///<summary>
  ///</summary>
  public static readonly Type TypeLongArray = typeof(long[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeULong = typeof(ulong);

  ///<summary>
  ///</summary>
  public static readonly Type TypeULongArray = typeof(ulong[]);

  ///<summary>
  ///</summary>
  public static readonly Type TypeDateTime = typeof(DateTime);
  public static readonly Type TypeTimeOnly = typeof(TimeOnly);

  ///<summary>
  ///</summary>
  public static readonly Type TypeDateTimeOffset = typeof(DateTimeOffset);

  ///<summary>
  ///</summary>
  public static readonly Type TypeTimeSpan = typeof(TimeSpan);

  ///<summary>
  ///</summary>
  public static readonly Type TypeGuid = typeof(Guid);

  ///<summary>
  ///</summary>
  public static readonly Type TypeDecimal = typeof(decimal);

  private delegate object ConvertDelegate(object value);

  private static readonly Dictionary<Type, ConvertDelegate> ConvertShort = new()
  {
    { Typer.TypeShort, value => value },
    { Typer.TypeUShort, value => (ushort) (short) value },
    { Typer.TypeInt, value => (int) (short) value },
    { Typer.TypeUInt, value => (uint) (short) value },
    { Typer.TypeFloat, value => (float) (short) value },
    { Typer.TypeDouble, value => (double) (short) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => (short) value != 0 },
    { Typer.TypeChar, value => (char) (short) value },
    { Typer.TypeByte, value => (byte) (short) value },
    { Typer.TypeLong, value => (long) (short) value },
    { Typer.TypeULong, value => (ulong) (short) value },
    { Typer.TypeDecimal, value => (decimal) (short) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertUShort = new()
  {
    { Typer.TypeShort, value => (short) (ushort) value },
    { Typer.TypeUShort, value => value },
    { Typer.TypeInt, value => (int) (ushort) value },
    { Typer.TypeUInt, value => (uint) (ushort) value },
    { Typer.TypeFloat, value => (float) (ushort) value },
    { Typer.TypeDouble, value => (double) (ushort) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => (ushort) value != 0 },
    { Typer.TypeChar, value => (char) (ushort) value },
    { Typer.TypeByte, value => (byte) (ushort) value },
    { Typer.TypeLong, value => (long) (ushort) value },
    { Typer.TypeULong, value => (ulong) (ushort) value },
    { Typer.TypeDecimal, value => (decimal) (ushort) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertInt = new()
  {
    { Typer.TypeShort, value => (short) (int) value },
    { Typer.TypeUShort, value => (ushort) (int) value },
    { Typer.TypeInt, value => value },
    { Typer.TypeUInt, value => (uint) (int) value },
    { Typer.TypeFloat, value => (float) (int) value },
    { Typer.TypeDouble, value => (double) (int) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => (int) value != 0 },
    { Typer.TypeChar, value => (char) (int) value },
    { Typer.TypeByte, value => (byte) (int) value },
    { Typer.TypeLong, value => (long) (int) value },
    { Typer.TypeULong, value => (ulong) (int) value },
    { Typer.TypeDecimal, value => (decimal) (int) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertUInt = new()
  {
    { Typer.TypeShort, value => (short) (uint) value },
    { Typer.TypeUShort, value => (ushort) (uint) value },
    { Typer.TypeInt, value => (int) (uint) value },
    { Typer.TypeUInt, value => value },
    { Typer.TypeFloat, value => (float) (uint) value },
    { Typer.TypeDouble, value => (double) (uint) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => (uint) value != 0 },
    { Typer.TypeChar, value => (char) (uint) value },
    { Typer.TypeByte, value => (byte) (uint) value },
    { Typer.TypeLong, value => (long) (uint) value },
    { Typer.TypeULong, value => (ulong) (uint) value },
    { Typer.TypeDecimal, value => (decimal) (uint) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertFloat = new()
  {
    { Typer.TypeShort, value => (short) (float) value },
    { Typer.TypeUShort, value => (ushort) (float) value },
    { Typer.TypeInt, value => (int) (float) value },
    { Typer.TypeUInt, value => (uint) (float) value },
    { Typer.TypeFloat, value => value },
    { Typer.TypeDouble, value => (double) (float) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => Math.Abs((float) value) > 0 },
    { Typer.TypeChar, value => (char) (float) value },
    { Typer.TypeByte, value => (byte) (float) value },
    { Typer.TypeLong, value => (long) (float) value },
    { Typer.TypeULong, value => (ulong) (float) value },
    { Typer.TypeDecimal, value => (decimal) (float) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertDouble = new()
  {
    { Typer.TypeShort, value => (short) (double) value },
    { Typer.TypeUShort, value => (ushort) (double) value },
    { Typer.TypeInt, value => (int) (double) value },
    { Typer.TypeUInt, value => (uint) (double) value },
    { Typer.TypeFloat, value => (float) (double) value },
    { Typer.TypeDouble, value => value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => Math.Abs((double) value) > 0 },
    { Typer.TypeChar, value => (char) (double) value },
    { Typer.TypeByte, value => (byte) (double) value },
    { Typer.TypeLong, value => (long) (double) value },
    { Typer.TypeULong, value => (ulong) (double) value },
    { Typer.TypeDecimal, value => (decimal) (double) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertDecimal = new()
  {
    { Typer.TypeShort, value => (short) (decimal) value },
    { Typer.TypeUShort, value => (ushort) (decimal) value },
    { Typer.TypeInt, value => (int) (decimal) value },
    { Typer.TypeUInt, value => (uint) (decimal) value },
    { Typer.TypeFloat, value => (float) (decimal) value },
    { Typer.TypeDouble, value => (double) (decimal) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => Math.Abs((decimal) value) > 0 },
    { Typer.TypeChar, value => (char) (decimal) value },
    { Typer.TypeByte, value => (byte) (decimal) value },
    { Typer.TypeLong, value => (long) (decimal) value },
    { Typer.TypeULong, value => (ulong) (decimal) value },
    { Typer.TypeDecimal, value => value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertChar = new()
  {
    { Typer.TypeShort, value => (short) (char) value },
    { Typer.TypeUShort, value => (ushort) (char) value },
    { Typer.TypeInt, value => (int) (char) value },
    { Typer.TypeUInt, value => (uint) (char) value },
    { Typer.TypeFloat, value => (float) (char) value },
    { Typer.TypeDouble, value => (double) (char) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => (char) value != 0 },
    { Typer.TypeChar, value => value },
    { Typer.TypeByte, value => (char) value },
    { Typer.TypeLong, value => (long) (char) value },
    { Typer.TypeULong, value => (ulong) (char) value },
    { Typer.TypeDecimal, value => (decimal) (char) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertByte = new()
  {
    { Typer.TypeShort, value => (short) (byte) value },
    { Typer.TypeUShort, value => (ushort) (byte) value },
    { Typer.TypeInt, value => (int) (byte) value },
    { Typer.TypeUInt, value => (uint) (byte) value },
    { Typer.TypeFloat, value => (float) (byte) value },
    { Typer.TypeDouble, value => (double) (byte) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => (byte) value != 0 },
    { Typer.TypeChar, value => (char) value },
    { Typer.TypeByte, value => value },
    { Typer.TypeLong, value => (long) (byte) value },
    { Typer.TypeULong, value => (ulong) (byte) value },
    { Typer.TypeDecimal, value => (decimal) (byte) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertLong = new()
  {
    { Typer.TypeShort, value => (short) (long) value },
    { Typer.TypeUShort, value => (ushort) (long) value },
    { Typer.TypeInt, value => (int) (long) value },
    { Typer.TypeUInt, value => (uint) (long) value },
    { Typer.TypeFloat, value => (float) (long) value },
    { Typer.TypeDouble, value => (double) (long) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => (long) value != 0 },
    { Typer.TypeChar, value => (char) (long) value },
    { Typer.TypeByte, value => (byte) (long) value },
    { Typer.TypeLong, value => value },
    { Typer.TypeULong, value => (ulong) (long) value },
    { Typer.TypeDecimal, value => (decimal) (long) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertULong = new()
  {
    { Typer.TypeShort, value => (short) (ulong) value },
    { Typer.TypeUShort, value => (ushort) (ulong) value },
    { Typer.TypeInt, value => (int) (ulong) value },
    { Typer.TypeUInt, value => (uint) (ulong) value },
    { Typer.TypeFloat, value => (float) (ulong) value },
    { Typer.TypeDouble, value => (double) (ulong) value },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => (ulong) value != 0 },
    { Typer.TypeChar, value => (char) (ulong) value },
    { Typer.TypeByte, value => (byte) (ulong) value },
    { Typer.TypeLong, value => (long) (ulong) value },
    { Typer.TypeULong, value => value },
    { Typer.TypeDecimal, value => (decimal) (ulong) value },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertBoolean = new()
  {
    { Typer.TypeShort, value => (bool) value ? (short) 1 : (short) 0 },
    { Typer.TypeUShort, value => (bool) value ? (ushort) 1 : (ushort) 0 },
    { Typer.TypeInt, value => (bool) value ? 1 : 0 },
    { Typer.TypeUInt, value => (bool) value ? 1u : 0u },
    { Typer.TypeFloat, value => (bool) value ? 1f : 0f },
    { Typer.TypeDouble, value => (bool) value ? 1d : 0d },
    { Typer.TypeString, value => value.ToString()! },
    { Typer.TypeBoolean, value => value },
    { Typer.TypeChar, value => (bool) value ? (char) 1 : (char) 0 },
    { Typer.TypeByte, value => (bool) value ? (byte) 1 : (byte) 0 },
    { Typer.TypeLong, value => (bool) value ? 1 : 0 },
    { Typer.TypeULong, value => (bool) value ? 1 : 0 },
    { Typer.TypeDecimal, value => (decimal)((bool) value ? 1 : 0) },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertString = new()
  {
    { Typer.TypeShort, value => { return short.TryParse((string)value, out var r) ? r : (short)TryOtherBase((string)value); } },
    { Typer.TypeUShort, value => { return ushort.TryParse((string)value, out var r) ? r : (ushort)TryOtherBase((string)value); } },
    { Typer.TypeInt, value => { return int.TryParse((string)value, out var r) ? r : (int)TryOtherBase((string)value); } },
    { Typer.TypeUInt, value => { return uint.TryParse((string)value, out var r) ? r : (uint)TryOtherBase((string)value); } },
    { Typer.TypeFloat, value => { return float.TryParse((string)value, out var r) ? r : (float)TryOtherBase((string)value); } },
    { Typer.TypeDouble, value => { return double.TryParse((string)value, out var r) ? r : (double)TryOtherBase((string)value); } },
    { Typer.TypeString, value => value },
    { Typer.TypeBoolean, value => ConvertHelper.BoolFromStr((string)value) },
    { Typer.TypeChar, value => { return char.TryParse((string)value, out var r) ? r : (char)TryOtherBase((string)value); } },
    { Typer.TypeByte, value => { return byte.TryParse((string)value, out var r) ? r : (byte)TryOtherBase((string)value); } },
    { Typer.TypeLong, value => { return long.TryParse((string)value, out var r) ? r : TryOtherBase((string)value); } },
    { Typer.TypeULong, value => { return ulong.TryParse((string)value, out var r) ? r : TryOtherBase((string)value); } },
    { Typer.TypeDecimal, value => { return decimal.TryParse((string)value, out var r) ? r : (decimal)TryOtherBase((string)value); } },
    { Typer.TypeDateTime, value => { return DateTime.TryParse((string)value, out var r) ? r : DateTime.MinValue; } },
    { Typer.TypeTimeOnly, value => { return TimeOnly.TryParse((string)value, out var r) ? r : TimeOnly.MinValue; } },
    { Typer.TypeTimeSpan, value => { return TimeSpan.TryParse((string)value, out var r) ? r : TimeSpan.MinValue; } },
    { Typer.TypeDateTimeOffset, value => { return DateTimeOffset.TryParse((string)value, out var r) ? r : DateTimeOffset.MinValue; } },
    { typeof(Type), value => Type.GetType((string)value)! },
  };

  private static long TryOtherBase(string value)
  {
    if (string.IsNullOrEmpty(value) || value.Length < 3)
      return 0;

    long r = 0;
    var prefix = value[..2];

    if (prefix.EqualsOrdinalIgnoreCase("0x"))
      try { r = System.Convert.ToInt64(value[2..], 16); } catch { }
    else if (prefix.EqualsOrdinalIgnoreCase("0b"))
      try { r = System.Convert.ToInt64(value[2..], 2); } catch { }
    else if (prefix.EqualsOrdinalIgnoreCase("0o"))
      try { r = System.Convert.ToInt64(value[2..], 8); } catch { }

    return r;
  }

  private static readonly Dictionary<Type, ConvertDelegate> ConvertObject = new()
  {
    { Typer.TypeShort, value => System.Convert.ToInt16(value) },
    { Typer.TypeUShort, value => System.Convert.ToUInt16(value) },
    { Typer.TypeInt, value => System.Convert.ToInt32(value) },
    { Typer.TypeUInt, value => System.Convert.ToUInt32(value) },
    { Typer.TypeFloat, value => System.Convert.ToSingle(value) },
    { Typer.TypeDouble, value => System.Convert.ToDouble(value) },
    { Typer.TypeString, System.Convert.ToString! },
    { Typer.TypeBoolean, value => System.Convert.ToBoolean(value) },
    { Typer.TypeChar, value => System.Convert.ToChar(value) },
    { Typer.TypeByte, value => System.Convert.ToByte(value) },
    { Typer.TypeLong, value => System.Convert.ToInt64(value) },
    { Typer.TypeULong, value => System.Convert.ToUInt64(value) },
    { Typer.TypeDecimal, value => System.Convert.ToDecimal(value) },
  };

  private static readonly Dictionary<Type, ConvertDelegate> ConvertCast = new();

  private static readonly Dictionary<Type, Dictionary<Type, ConvertDelegate>> Map = new()
  {
    { Typer.TypeShort, ConvertShort },
    { Typer.TypeUShort, ConvertUShort },
    { Typer.TypeInt, ConvertInt },
    { Typer.TypeUInt, ConvertUInt },
    { Typer.TypeFloat, ConvertFloat },
    { Typer.TypeDouble, ConvertDouble },
    { Typer.TypeString, ConvertString },
    { Typer.TypeBoolean, ConvertBoolean },
    { Typer.TypeChar, ConvertChar },
    { Typer.TypeByte, ConvertByte },
    { Typer.TypeLong, ConvertLong },
    { Typer.TypeULong, ConvertULong },
    { Typer.TypeObject, ConvertObject },
    { Typer.TypeDecimal, ConvertDecimal },
  };

  private static readonly ConvertDelegate DefConv = value => value;
  private static readonly MethodInfo CastToInfo = typeof(Typer).GetMethod("CastTo", BindingFlags.NonPublic | BindingFlags.Static)!;

#pragma warning disable IDE0051 // Remove unused private members
  private static object? CastTo<T>(object value) where T : class // it is used by reflection
  {
    return value as T;
  }
#pragma warning restore IDE0051 // Remove unused private members

  private static ConvertDelegate GetConvertCast(Type type)
  {
    lock (ConvertCast)
      return ConvertCast.TryGetValue(type, out var d) ? d : CreateConvertCast(type);
  }

  private static ConvertDelegate CreateConvertCast(Type type)
  {
    var d = MethodGenerator.MakeGenericDelegate<ConvertDelegate>(CastToInfo, type);
    ConvertCast.Add(type, d);
    return d;
  }

  private static ConvertDelegate GetConverter(Type srct, Type dstt)
  {
    return Map.TryGetValue(srct, out var srcf) && srcf.TryGetValue(dstt, out var srcd) ? srcd : dstt == Typer.TypeObject || dstt.IsValueType ? DefConv : GetConvertCast(dstt);
  }

  private static object Convert(object value, Type srct, Type dstt)
  {
    return GetConverter(srct, dstt)(value);
  }

  /// <summary>
  /// Returns a default value for a given type
  /// </summary>
  /// <param name="type">Desired type</param>
  /// <param name="arrayLength">Optional length for arrays</param>
  /// <returns></returns>
  public static object? DefValue(Type? type, int arrayLength = 0)
  {
    return type == null ? null : type.IsArray ? Array.CreateInstance(type.GetElementType()!, arrayLength) : type == Typer.TypeString ? string.Empty : Activator.CreateInstance(type);
  }

  public static T? ToArray<T>(object? srcval, int arrayLength = 0)
  {
    return (T?)ToArray(typeof(T), srcval, arrayLength);
  }

  public static object? ToArray(Type? dsttype, object? srcval, int arrayLength = 0)
  {
    return Cast(dsttype, srcval, null, arrayLength);
  }

  /// <summary>
  /// Convert a value to desired type
  /// </summary>
  /// <typeparam name="T">Desired result type</typeparam>
  /// <param name="srcval">Value to convert</param>
  /// <param name="arrayLength">Optional length for arrays</param>
  /// <returns></returns>
  public static T? To<T>(object? srcval, T? defaultValue = default)
  {
    return (T?)Cast(typeof(T), srcval, defaultValue, 0);
  }

  public static object? Cast(Type? dsttype, object? srcval, object? defaultValue = null, int arrayLength = 0)
  {
    return Cast(dsttype, srcval, arrayLength) ?? defaultValue ?? DefValue(dsttype, arrayLength);
  }

  public static object? Cast(Type? dsttype, object? srcval, int arrayLength)
  {
    try
    {
      if (dsttype == null || srcval == null || srcval is System.DBNull)
        return DefValue(dsttype, arrayLength);

      if (dsttype.IsArray)
        return ConvertArray(srcval, dsttype, arrayLength);

      var srctype = srcval.GetType();

      if (dsttype.IsEnum && srctype == Typer.TypeString)
      {
        var value = (string)srcval;
        var index = Array.FindIndex(dsttype.GetEnumNames(), i => i.EqualsOrdinalIgnoreCase(value));
        return index >= 0 ? index : null;
      }

      if (typeof(ITuple).IsAssignableFrom(dsttype))
      {
                var ga = dsttype.GenericTypeArguments;
                
                if (srctype == Typer.TypeString)
                {
                    var values = ((string)srcval).TrimStart('(').TrimEnd(')').Split(',');
                    if (values.Length != ga.Length)
                        return null;
                    var dstvalues = new object?[values.Length];
                    for (var i = 0; i < values.Length; i++)
                        dstvalues[i] = Cast(ga[i], values[i]);
                    return Activator.CreateInstance(dsttype, dstvalues);
                }
                else if (srctype.IsArray)
                {
                    var aa = ArrayAccessor.ForObject();
                    var valuesLength = aa.GetLength(srcval);
                    if (valuesLength != ga.Length)
                        return null;
                    var dstvalues = new object?[valuesLength];
                    for (var i = 0; i < valuesLength; i++)
                        dstvalues[i] = Cast(ga[i], aa.Get(srcval, i));
                    return Activator.CreateInstance(dsttype, dstvalues);
                }
            }

        if (srcval is not Array srca)
            return srctype == dsttype ? srcval : Convert(srcval, srctype, dsttype);

        if (srca.Length == 0 || (srcval = srca.GetValue(0)!) == null)
            return DefValue(dsttype);

        return (srctype = srctype.GetElementType()) == dsttype ? srcval : Convert(srcval, srctype!, dsttype);
    }
    catch
    {
      return null;
    }
  }

  private static object? ConvertArray(object src, Type dstType, int maxLen)
  {
    if (!dstType.IsArray || (dstType = dstType.GetElementType()!) == null)
      return null;

    Array dstArray;
    var dstAccessor = ArrayAccessor.For(dstType);

    if (src is Array srcArray)
    {
      var srcAccessor = ArrayAccessor.For(src);
      var srcType = srcAccessor.GetElementType();
      var srcLength = srcArray.Length;

      if (srcType == dstType && (maxLen == 0 || srcLength == maxLen))
        return src;

      var dstLength = maxLen > 0 ? Math.Min(srcLength, maxLen) : srcLength;

      dstArray = dstAccessor.Create(maxLen > 0 ? maxLen : srcLength);

      if (dstType != Typer.TypeObject && srcType != dstType)
      {
        var conv = GetConverter(srcType, dstType);

        for (var i = 0; i < dstLength; i++)
          dstAccessor.Set(dstArray, i, conv(srcAccessor.Get(src, i)));
      }
      else
        Array.Copy(srcArray, 0, dstArray, 0, dstLength);
    }
    else
    {
      dstArray = dstAccessor.Create(Math.Max(1, maxLen));

      if (src != null)
        dstAccessor.Set(dstArray, 0, Convert(src, src.GetType(), dstType));
    }

    return dstArray;
  }
}
