namespace Antiriad.Core.Collections;

using System;
using System.Linq;
using Antiriad.Core.Helpers;

/// <summary>
/// Static class for array manipulation.
/// ArrayAccessor is way faster than Array SetValue()/GetValue() methods
/// </summary>
public abstract class ArrayAccessor
{
  public delegate object GetFromBytesDelegate(ReadOnlySpan<byte> array, int index, bool le);
  public delegate int GetBytesFromDelegate(object value, Span<byte> buffer, int index, bool le = true);

  /// <summary>
  /// Common interface for ArrayAccessor
  /// </summary>
  private sealed class Accessor<T> : ArrayAccessor where T : IEquatable<T>
  {
    public Accessor(int elementSize, GetFromBytesDelegate? bt, GetBytesFromDelegate? bf)
    {
      this.ElementSize = elementSize;
      this.GetValueFromBytes = bt;
      this.GetBytesFromValue = bf;
    }

    public override GetFromBytesDelegate? GetValueFromBytes { get; protected set; }
    public override GetBytesFromDelegate? GetBytesFromValue { get; protected set; }
    public override int ElementSize { get; protected set; }
    public override Array Create(int size) { return new T[size]; }
    public override Type GetElementType() { return typeof(T); }
    public override Type GetObjectType() { return typeof(T[]); }
    public override int GetLength(object a) { return ((T[])a).Length; }
    public override object Get(object a, int index) { return ((T[])a)[index]; }
    public override void Set(object a, int index, object value) { ((T[])a)[index] = (T)value; }
    public override object AsList(object a) { return ((T[])a).ToList(); }

    public override bool ElementEquals(object a1, object a2)
    {
      var array1 = (T[])a1;
      var array2 = (T[])a2;

      if (array1 == array2) return true;
      if (array1 == null || array2 == null || array1.Length != array2.Length) return false;

      for (var i = 0; i < array1.Length; i++)
        if (!array1[i].Equals(array2[i])) return false;

      return true;
    }

    public override int IndexOf(object a, object e, bool reversed)
    {
      var array = (T[])a;

      if (reversed)
        for (var i = array.Length - 1; i >= 0; i--) { if (array[i].Equals(e)) return i; }
      else
        for (var i = 0; i < array.Length; i++) { if (array[i].Equals(e)) return i; }

      return -1;
    }
  }

  private sealed class GenericAccessor : ArrayAccessor
  {
    public GenericAccessor(int elementSize, GetFromBytesDelegate bf, GetBytesFromDelegate bt)
    {
      this.ElementSize = elementSize;
      this.GetValueFromBytes = bf;
      this.GetBytesFromValue = bt;
    }

    public override GetFromBytesDelegate? GetValueFromBytes { get; protected set; }
    public override GetBytesFromDelegate? GetBytesFromValue { get; protected set; }
    public override int ElementSize { get { return 0; } protected set { } }
    public override Array Create(int size) { return new object[size]; }
    public override Type GetElementType() { return Typer.TypeObject; }
    public override Type GetObjectType() { return Typer.TypeObjectArray; }
    public override int GetLength(object a) { return ((object[])a).Length; }
    public override object Get(object a, int index) { return ((object[])a)[index]; }
    public override void Set(object a, int index, object value) { ((object[])a)[index] = value; }
    public override object AsList(object a) { return ((object[])a).ToList(); }

    public override bool ElementEquals(object a1, object a2)
    {
      var array1 = (object[])a1;
      var array2 = (object[])a2;

      if (array1 == array2) return true;
      if (array1 == null || array2 == null || array1.Length != array2.Length) return false;

      for (var i = 0; i < array1.Length; i++)
        if (!array1[i].Equals(array2[i])) return false;

      return true;
    }

    public override int IndexOf(object a, object e, bool reversed)
    {
      var array = (object[])a;

      if (reversed)
        for (var i = array.Length - 1; i >= 0; i--) { if (array[i].Equals(e)) return i; }
      else
        for (var i = 0; i < array.Length; i++) { if (array[i].Equals(e)) return i; }

      return -1;
    }
  }

  private static ArrayAccessor? booleanaa;
  private static ArrayAccessor? stringaa;
  private static ArrayAccessor? byteaa;
  private static ArrayAccessor? charaa;
  private static ArrayAccessor? doubleaa;
  private static ArrayAccessor? floataa;
  private static ArrayAccessor? intaa;
  private static ArrayAccessor? longaa;
  private static ArrayAccessor? ulongaa;
  private static ArrayAccessor? shortaa;
  private static ArrayAccessor? uintaa;
  private static ArrayAccessor? ushortaa;
  private static ArrayAccessor? objectaa;

  public abstract GetFromBytesDelegate? GetValueFromBytes { get; protected set; }
  public abstract GetBytesFromDelegate? GetBytesFromValue { get; protected set; }
  public abstract int ElementSize { get; protected set; }
  public abstract Array Create(int size);
  public abstract Type GetElementType();
  public abstract Type GetObjectType();
  public abstract int GetLength(object a);
  public abstract object Get(object a, int index);
  public abstract void Set(object a, int index, object value);
  public abstract object AsList(object a);
  public abstract bool ElementEquals(object a1, object a2);
  public abstract int IndexOf(object a, object e, bool reversed);

  public byte[] ToBytes(Array a, bool le = true)
  {
    return this.ToBytes(a, 0, a.Length, le);
  }

  public byte[] ToBytes(Array a, int index, int count, bool le = true)
  {
    var array = new byte[count * this.ElementSize];
    this.ToBytes(a, index, count, array, 0, le);
    return array;
  }

  public int ToBytes(Array src, int srcIndex, int srcCount, Span<byte> dst, int dstIndex, bool le = true)
  {
    if (this.GetBytesFromValue == null)
      throw new NotSupportedException($"ToBytes: conversion not supported. type={this.GetObjectType()}");

    var size = this.ElementSize;

    for (var i = 0; i < srcCount; i++)
    {
      var index = dstIndex + i * size;

      if (index + size <= dst.Length)
        this.GetBytesFromValue(this.Get(src, i + srcIndex), dst, index, le);
    }

    return srcCount * size;
  }

  public Array FromBytes(ReadOnlySpan<byte> a, bool le = true)
  {
    return this.FromBytes(a, 0, a.Length / this.ElementSize, le);
  }

  public Array FromBytes(ReadOnlySpan<byte> a, int index, int count, bool le = true)
  {
    if (this.GetValueFromBytes == null)
      throw new NotSupportedException($"FromBytes: conversion not supported. type={this.GetObjectType()}");

    var array = this.Create(count);
    var size = this.ElementSize;

    for (var i = 0; i < array.Length; i++)
      this.Set(array, i, this.GetValueFromBytes(a, index + i * size, le));

    return array;
  }

  /// <summary>
  /// Get an ArrayAccessor from object type
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  public static ArrayAccessor For(object value)
  {
    return For(value.GetType());
  }

  /// <summary>
  /// Get an ArrayAccessor from type
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static ArrayAccessor For(Type type)
  {
    if (type == Typer.TypeBoolArray || type == Typer.TypeBoolean) return ForBool();
    if (type == Typer.TypeByteArray || type == Typer.TypeByte) return ForByte();
    if (type == Typer.TypeCharArray || type == Typer.TypeChar) return ForChar();
    if (type == Typer.TypeDoubleArray || type == Typer.TypeDouble) return ForDouble();
    if (type == Typer.TypeFloatArray || type == Typer.TypeFloat) return ForFloat();
    if (type == Typer.TypeIntArray || type == Typer.TypeInt) return ForInt();
    if (type == Typer.TypeUIntArray || type == Typer.TypeUInt) return ForUInt();
    if (type == Typer.TypeLongArray || type == Typer.TypeLong) return ForLong();
    if (type == Typer.TypeULongArray || type == Typer.TypeULong) return ForULong();
    if (type == Typer.TypeShortArray || type == Typer.TypeShort) return ForShort();
    if (type == Typer.TypeUShortArray || type == Typer.TypeUShort) return ForUShort();
    if (type == Typer.TypeStringArray || type == Typer.TypeString) return ForString();
    return ForObject();
  }

  public static ArrayAccessor ForBool()
  {
    return booleanaa ??= new Accessor<bool>(sizeof(bool), (a, i, l) => a[i], (v, a, i, l) => { a[i] = (bool)v ? (byte)1 : (byte)0; return 1; });
  }

  public static ArrayAccessor ForByte()
  {
    return byteaa ??= new Accessor<byte>(sizeof(byte), (a, i, l) => a[i], (v, a, i, l) => { a[i] = (byte)v; return 1; });
  }

  public static ArrayAccessor ForChar()
  {
    return charaa ??= new Accessor<char>(sizeof(char), (a, i, l) => a[i], (v, a, i, l) => { a[i] = (byte)v; return 1; });
  }

  public static ArrayAccessor ForDouble()
  {
    return doubleaa ??= new Accessor<double>(sizeof(double), null, null);
  }

  public static ArrayAccessor ForFloat()
  {
    return floataa ??= new Accessor<float>(sizeof(float), null, null);
  }

  public static ArrayAccessor ForInt()
  {
    return intaa ??= new Accessor<int>(sizeof(int), (a, i, l) => Bytes.ToInt(a[i..], l), (v, a, i, l) => Bytes.FromInt((int)v, a[i..], l));
  }

  public static ArrayAccessor ForUInt()
  {
    return uintaa ??= new Accessor<uint>(sizeof(uint), (a, i, l) => Bytes.ToUInt(a[i..], l), (v, a, i, l) => Bytes.FromUInt((uint)v, a[i..], l));
  }

  public static ArrayAccessor ForLong()
  {
    return longaa ??= new Accessor<long>(sizeof(long), (a, i, l) => Bytes.ToLong(a[i..], l), (v, a, i, l) => Bytes.FromLong((long)v, a[i..], l));
  }

  public static ArrayAccessor ForULong()
  {
    return ulongaa ??= new Accessor<ulong>(sizeof(ulong), (a, i, l) => Bytes.ToULong(a[i..], l), (v, a, i, l) => Bytes.FromULong((ulong)v, a[i..], l));
  }

  public static ArrayAccessor ForShort()
  {
    return shortaa ??= new Accessor<short>(sizeof(short), (a, i, l) => Bytes.ToShort(a[i..], l), (v, a, i, l) => Bytes.FromShort((short)v, a[i..], l));
  }

  public static ArrayAccessor ForUShort()
  {
    return ushortaa ??= new Accessor<ushort>(sizeof(ushort), (a, i, l) => Bytes.ToUShort(a[i..], l), (v, a, i, l) => Bytes.FromUShort((ushort)v, a[i..], l));
  }

  public static ArrayAccessor ForString()
  {
    return stringaa ??= new Accessor<string>(0, (a, i, l) => a[i], (v, a, i, l) => 0);
  }

  public static ArrayAccessor ForObject()
  {
    return objectaa ??= new GenericAccessor(0, (a, i, l) => a[i], (v, a, i, l) => 0);
  }
}
