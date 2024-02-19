using System.Collections;
using Antiriad.Core.Collections;
using Antiriad.Core.Helpers;
using Antiriad.Core.Log;
using Antiriad.Core.Serialization.Tool;

namespace Antiriad.Core.Serialization;

public class NaibSerializer
{
  private readonly NaibTypeInfoList cache;
  private readonly List<object> objects = new();
  private readonly bool sharedCache;
  private NaibStream? stream;

  public NaibSerializer() : this(null, false) { }

  public NaibSerializer(NaibTypeInfoList? cache, bool sharedCache)
  {
    this.cache = cache ?? new NaibTypeInfoList();
    this.sharedCache = sharedCache;
  }

  public static byte[] Encode(object data)
  {
    var e1 = new NaibSerializer();
    var m1 = new MemoryStream();
    e1.Write(m1, data);
    return m1.ToArray();
  }

  public void Write(Stream mem, object data)
  {
    this.objects.Clear();
    this.stream = new NaibStream(mem);
    this.stream.WriteVersion();

    var type = data.GetType();

    if (type.IsArray)
      this.Write(type.GetElementType()!, data);
    else if (type.IsPrimitive)
      this.Write(type, data);
    else
      this.Write(Typer.TypeObject, data);
  }

  private void Write(Type? type, object? data)
  {
    if (this.stream == null)
      throw new Exception("stream not assigned");

    var unit = Units.FromType(type);

    if (data == null)
    {
      this.stream.WriteValue(Typer.TypeByte, (byte)0);
      return;
    }

    var dataClass = data.GetType(); // _ATTTTTT
    var isArray = dataClass.IsArray || data is ICollection || (type != Typer.TypeString && data is IEnumerable);

    if (isArray)
    {
      var etype = dataClass.GetElementType();

      if (dataClass.IsGenericType)
      {
        var ga = dataClass.GetGenericArguments();

        if (ga != null && ga.Any())
        {
          etype = type = ga[0];
          unit = Units.FromType(type);
        }
      }

      if (etype != null && etype.IsEnum)
        unit = Unit.Int;
    }

    this.stream.WriteByte((byte)((isArray ? NaibStream.ArrayMask : 0) | ((byte)unit & NaibStream.DataTypeMask))); // metadata + type

    if (isArray)
      this.WriteArray(type, data);
    else if (unit == Unit.Object)
      this.WriteObject(dataClass, data);
    else
      this.stream.WriteValue(type, data);
  }

  private void WriteObject(Type dataClass, object data)
  {
    var info = this.cache.Store(dataClass);
    var needId = info.LocalId == -1;
    var isnew = needId || this.sharedCache;

    if (isnew)
    {
      if (needId) this.cache.GetId(info);
      this.stream!.WriteShort((short)(info.LocalId | NaibStream.NewObjectMask)); // typeid with typedef
      this.stream.WriteString(info.Name); // typename
      this.stream.WriteSize(info.Props.Length);
    }
    else
    {
      var index = this.objects.FindIndex(i => object.ReferenceEquals(i, data));

      if (index >= 0) // repeated instance
      {
        this.stream!.WriteShort((short)(info.LocalId | NaibStream.NewObjectMask)); // typeid with typedef
        this.stream.WriteString(null);
        this.stream.WriteShort((short)index);
        return;
      }

      this.stream!.WriteShort(info.LocalId); // typeid only
    }

    this.objects.Add(data);

    foreach (var field in info.Props)
    {
      try
      {
        var propType = Typer.TypeObject;
        var eval = field.Getter(data);
        var fieldType = eval != null ? eval.GetType() : field.DataType;

        if (fieldType.IsEnum)
        {
          propType = Typer.TypeInt;
        }
        else if (fieldType.IsArray)
        {
          propType = fieldType.GetElementType();
        }
        else if (eval != null && (propType = fieldType) == Typer.TypeObject)
        {
          if (eval is IEnumerable iter)
          {
            var ga = iter.GetType().GetGenericArguments();
            propType = ga.Length > 0 ? ga[0] : Typer.TypeObject;
          }
        }

        if (isnew) this.stream.WriteString(field.Name);
        this.stream.WriteShort(field.LocalId);
        this.Write(propType, eval);
      }
      catch
      {
        var name = field != null ? field.Name : "?";
        Trace.Error($"cannot serialize field={name}");
        this.Write(Typer.TypeObject, null);
      }
    }
  }

  private void WriteArray(Type? type, object? data)
  {
    var count = CollectionHelper.GetCount(data);

    this.stream!.WriteSize(count);

    if (count == 0 || type == null || data == null)
      return;

    if (type.IsArray)
      type = type.GetElementType();

    if (type == Typer.TypeByte) this.stream.WriteByteArray(data);
    else if (type == Typer.TypeChar) this.stream.WriteCharArray(data);
    else if (type == Typer.TypeBoolean) this.stream.WriteBoolArray(data);
    else if (type == Typer.TypeShort) this.stream.WriteShortArray(data);
    else if (type == Typer.TypeUShort) this.stream.WriteUShortArray(data);
    else if (type == Typer.TypeInt) this.stream.WriteIntArray(data);
    else if (type == Typer.TypeUInt) this.stream.WriteUIntArray(data);
    else if (type == Typer.TypeLong) this.stream.WriteLongArray(data);
    else if (type == Typer.TypeULong) this.stream.WriteULongArray(data);
    else if (type == Typer.TypeFloat) this.stream.WriteSingleArray(data);
    else if (type == Typer.TypeDouble) this.stream.WriteDoubleArray(data);
    else if (type == Typer.TypeString) this.stream.WriteStringArray(data);
    else if (type == Typer.TypeDateTime) this.stream.WriteDateTimeArray(data);
    else if (type == Typer.TypeDateTimeOffset) this.stream.WriteBiasedDateTimeArray(data);
    else if (type == Typer.TypeGuid) this.stream.WriteGuidArray(data);
    else if (type!.IsEnum) this.stream.WriteEnumArray(data);
    else
    {
      if (data is Array)
      {
        var a = (object[])data;

        for (var i = 0; i < count; i++)
        {
          var o = a[i];
          this.Write(o == null ? type : o.GetType(), o);
        }
      }
      else if (data is IEnumerable collection)
      {
        foreach (var o in collection)
        {
          this.Write(o == null ? type : o.GetType(), o);
        }
      }
    }
  }
}
