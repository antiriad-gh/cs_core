using System.Reflection;

namespace Antiriad.Core.Types;

internal class MemberMetadata
{
  private static readonly List<MemberMetadata> MemberCache = new();
  private static readonly object Locker = new();

  public Type DataType;
  internal GetHandler Getter;
  internal SetHandler Setter;
  internal MemberInfo Info;

  private static MemberMetadata AppendItem(MemberInfo info, GetHandler getter, SetHandler setter, Type dataType)
  {
    var item = new MemberMetadata
    {
      Info = info,
      Getter = getter,
      Setter = setter,
      DataType = dataType
    };

    MemberCache.Add(item);
    return item;
  }

  public static MemberMetadata Get(Type type, PropertyInfo info)
  {
    lock (Locker)
    {
      var item = MemberCache.Find(i => i.Info.DeclaringType == type && i.Info == info);
      if (item != null) return item;

      var getter = MethodGenerator.MakeGetHandler(type, info);
      var setter = MethodGenerator.MakeSetHandler(type, info);
      return AppendItem(info, getter, setter, info.PropertyType);
    }
  }

  public static MemberMetadata Get(Type type, FieldInfo info)
  {
    lock (Locker)
    {
      var item = MemberCache.Find(i => i.Info.DeclaringType == type && i.Info == info);
      if (item != null) return item;

      var getter = MethodGenerator.MakeGetHandler(type, info);
      var setter = MethodGenerator.MakeSetHandler(type, info);
      return AppendItem(info, getter, setter, info.FieldType);
    }
  }
}
