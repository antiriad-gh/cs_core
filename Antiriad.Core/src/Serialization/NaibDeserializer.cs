using System.Collections;
using System.Reflection;
using Antiriad.Core.Collections;
using Antiriad.Core.Helpers;
using Antiriad.Core.Log;
using Antiriad.Core.Serialization.Tool;
using Antiriad.Core.Types;

namespace Antiriad.Core.Serialization;

public class NaibDeserializer
{
  private const int EInvalidArgument = -2147024809;

  private readonly NaibTypeInfoList cache;
  private readonly List<object> objects = new();
  private NaibStream stream;
  private short version;
  private MethodInfo dictAddm;
  private bool dictAddmSearched;

  public NaibDeserializer() : this(null) { }

  public NaibDeserializer(NaibTypeInfoList cache)
  {
    this.cache = cache ?? new NaibTypeInfoList();
  }

  public static T Decode<T>(byte[] data)
  {
    var e1 = new NaibDeserializer();
    var m1 = new MemoryStream(data);
    return e1.Read<T>(m1);
  }

  public T Read<T>(Stream mem)
  {
    this.objects.Clear();
    this.stream = new NaibStream(mem);
    this.version = this.stream.ReadVersion();
    return (T)this.Read(typeof(T));
  }

  public object Read(Stream mem)
  {
    return this.Read<object>(mem);
  }

  private object Read(Type destType)
  {
    var dataType = this.stream.ReadByte();
    var type = Units.FromUnit((Unit)(dataType & NaibStream.DataTypeMask));

    if ((dataType & NaibStream.ArrayMask) > 0)
    {
      return this.ReadArray(type, destType);
    }

    if (type == Typer.TypeObject)
    {
      return this.ReadObject();
    }

    if (dataType == 0)
    {
      return null;
    }

    var value = this.stream.ReadValue(type);

    if (type == Typer.TypeDateTimeOffset && destType == Typer.TypeDateTime)
    {
      value = ((DateTimeOffset)value).DateTime;
    }

    return type != null && type.IsPrimitive ? Typer.Cast(destType, value) : value;
  }

  private object ReadObject()
  {
    var remoteid = this.stream.ReadShort();
    var isnew = (remoteid & NaibStream.NewObjectMask) > 0;
    NaibTypeInfo info;

    if (isnew)
    {
      var infoname = this.stream.ReadString();
      var infoid = (short)(remoteid & NaibStream.InfoIdMask);

      if (infoname == null) // repeated instance
      {
        var index = this.stream.ReadShort(); // reference index
        return index >= 0 && index < this.objects.Count ? this.objects[index] : null;
      }

      if ((info = this.cache.FindOrCreate(infoname, infoid)) == null)
      {
        Trace.Error($"class not found: name={infoname} id={infoid}");
        return null;
      }

      info.RemotePropCount = this.stream.ReadSize();
    }
    else if ((info = this.cache.Find(remoteid)) == null)
    {
      Trace.Error($"class not found: remoteid={remoteid}");
      return null;
    }

    var obj = info.NewInstance();
    var props = info.Props;

    this.objects.Add(obj);

    for (var i = 0; i < info.RemotePropCount; i++)
    {
      PropertyMetadata prop = null;
      var propName = isnew ? this.stream.ReadString() : null;
      var propIndex = this.stream.ReadShort();

      try
      {
        if (propName != null)
        {
          prop = props.Find(p => p.Name.EqualsOrdinalIgnoreCase(propName));

          if (prop != null)
            prop.RemoteId = propIndex;
          else
            Trace.Error($"property name={propName} not found at class={obj.GetType().Name}");
        }
        else
          prop = props.Find(p => p.RemoteId == propIndex);

        var propValue = this.Read(prop?.DataType);
        prop?.Set(obj, propValue);
      }
      catch (Exception ex)
      {
        var hresult = System.Runtime.InteropServices.Marshal.GetHRForException(ex);

        if (hresult != EInvalidArgument)
        {
          Trace.Error($"cannot deserialize field={propName ?? (prop != null ? prop.Name : "?")}");
        }
      }
    }

    return obj;
  }

  private object? ReadArray(Type listType, Type destType)
  {
    var count = this.stream.ReadSize();

    if (count <= 0 || listType == null || destType == null)
      return null;

    var createArray = destType.IsArray || destType == Typer.TypeObject;

    if (listType == Typer.TypeByte) return this.stream.ReadByteArray(createArray, count);
    if (listType == Typer.TypeBoolean) return this.stream.ReadBoolArray(createArray, count);
    if (listType == Typer.TypeChar) return this.stream.ReadCharArray(createArray, count);
    if (listType == Typer.TypeShort) return this.stream.ReadShortArray(createArray, count);
    if (listType == Typer.TypeUShort) return this.stream.ReadUShortArray(createArray, count);

    if (listType == Typer.TypeInt)
    {
      var etype = destType.GetElementType();

      if (etype != null && etype.IsEnum)
        return this.stream.ReadEnumArray(etype, count);

      var intga = destType.GetGenericArguments();

      if (intga.Length > 0 && intga[0].IsEnum)
        return this.stream.ReadEnumList(intga[0], count);

      return this.stream.ReadIntArray(createArray, count);
    }

    if (listType == Typer.TypeUInt) return this.stream.ReadUIntArray(createArray, count);
    if (listType == Typer.TypeFloat) return this.stream.ReadSingleArray(createArray, count);
    if (listType == Typer.TypeLong) return this.stream.ReadLongArray(createArray, count);
    if (listType == Typer.TypeULong) return this.stream.ReadULongArray(createArray, count);
    if (listType == Typer.TypeDouble) return this.stream.ReadDoubleArray(createArray, count);
    if (listType == Typer.TypeString) return this.stream.ReadStringArray(createArray, count);
    if (listType == Typer.TypeDateTime) return this.stream.ReadDateTimeArray(createArray, count);
    if (listType == Typer.TypeDateTimeOffset) return this.stream.ReadBiasedDateTimeArray(createArray, count);
    if (listType == Typer.TypeGuid) return this.stream.ReadGuidArray(createArray, count);

    if (createArray)
    {
      destType = destType.GetElementType() ?? Typer.TypeObject;
      var array = destType == Typer.TypeObject ? null : Array.CreateInstance(destType, count);
      Type arrayType = null;

      for (var i = 0; i < count; i++)
      {
        var item = this.Read(destType);

        if (item == null)
        {
          continue;
        }

        var itemType = item.GetType();

        if (array == null)
        {
          arrayType = itemType;
          array = Array.CreateInstance(itemType, count);
        }

        if (arrayType != null && itemType != arrayType && !arrayType.IsAssignableFrom(itemType))
        {
          var tmp = Array.CreateInstance(destType, count);
          array.CopyTo(tmp, 0);
          array = tmp;
        }

        array.SetValue(item, i);
      }

      return array ?? Array.CreateInstance(destType, count);
    }

    if (typeof(IDictionary).IsAssignableFrom(destType))
    {
      if (!this.dictAddmSearched)
      {
        this.dictAddmSearched = true;
        this.dictAddm = destType.GetMethod("System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Add", BindingFlags.NonPublic | BindingFlags.Instance);
      }

      if (this.dictAddm != null)
      {
        var dict = Activator.CreateInstance(destType);

        for (var i = 0; i < count; i++)
        {
          this.dictAddm.Invoke(dict, new[] { this.Read(Typer.TypeObject) });
        }

        return dict;
      }
    }

    var ga = destType.GetGenericArguments();
    destType = ga.Length > 0 ? ga[0] : Typer.TypeObject;
    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new[] { destType }), count);

    for (var i = 0; i < count; i++)
    {
      list.Add(this.Read(destType));
    }

    return list;
  }
}
