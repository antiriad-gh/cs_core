using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Antiriad.Core.Collections;
using Antiriad.Core.Helpers;
using Antiriad.Core.Log;
using Antiriad.Core.Types;

namespace Antiriad.Core.Serialization.Tool;

internal class NaibTypeInfo
{
  /*
0111 1110 <   0 byte  - < 127
0111 1111     1 byte  - < 256 -> ubyte     ff
1000 0000     2 bytes - < 192 -> ushort <= f ffff
1100 0000     4 bytes - > 191 -> uint   <= f ffff ffff
*/

  public short LocalId;
  public short RemoteId;
  public int RemotePropCount;
  public Type Type;
  public string Name;
  public PropertyMetadata[] Props;
  public ConstructorHandler Constructor;

  internal NaibTypeInfo(Type type, short localid, short remoteid)
  {
    this.LocalId = localid;
    this.RemoteId = remoteid;
    this.Type = type;
    this.Name = NaibTypeInfo.GetAliasFromType(this.Type.FullName);

    var metap = new List<PropertyMetadata>();

    try
    {
      var list = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
      metap.AddRange(list.Where(i => i.GetCustomAttributes(typeof(CompilerGeneratedAttribute), true).Length == 0).OrderBy(i => i.Name).Select(i => new PropertyMetadata(this.Type, i)));

      // getter only fields has
      // attribute: CompilerGeneratedAttribute
      // name: <FieldName>k__BackingField
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }

    try
    {
      var list = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
      metap.AddRange(list.Where(i => i.CanWrite).OrderBy(i => i.Name).Select(i => new PropertyMetadata(this.Type, i)));
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }

    metap.ForEach((i, e) => e.LocalId = (short)i);

    if (type.IsArray || type.IsPrimitive || type == Typer.TypeString)
      this.Constructor = () => Activator.CreateInstance(type);
    else if (type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null)
      this.Constructor = MethodGenerator.MakeConstructorHandler(type);
    else
      this.Constructor = () => FormatterServices.GetUninitializedObject(type);

    this.Props = metap.ToArray();
  }

  public object NewInstance()
  {
    try
    {
      return this.Constructor();
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }

    return null;
  }

  private class HashPair
  {
    public readonly string Alias;
    public int Id;

    public HashPair(string alias, int id)
    {
      this.Alias = alias;
      this.Id = id;
    }
  }

  private static readonly Dictionary<string, HashPair> HashAliases = new();
  private static readonly object HashLock = new();

  public static void Register(Type type)
  {
    lock (HashLock)
    {
      foreach (var c in type.GetNestedTypes())
      {
        var ca = (RegisterType[])c.GetCustomAttributes(typeof(RegisterType), false);
        if (ca.Length > 0) NaibTypeInfo.Register(c, string.IsNullOrEmpty(ca[0].Name) ? c.Name : ca[0].Name, ca[0].Id);
      }
    }
  }

  public static int Register(Type type, string alias, int id)
  {
    lock (HashLock)
    {
      var name = type.FullName;

      if (!HashAliases.TryGetValue(name, out HashPair pair))
      {
        HashAliases.Add(name, pair = new HashPair(alias, id));
        if (pair.Id == 0) pair.Id = NaibStream.CalculateHash(alias);
        Trace.Debug($"register class={name} alias={alias} id={pair.Id}");
      }

      return pair.Id;
    }
  }

  public static int GetHash(object value)
  {
    return GetHash(value.GetType());
  }

  public static int GetHash(Type type)
  {
    return NaibStream.CalculateHash(type.FullName);
  }

  public static string GetTypeFromAlias(string value)
  {
    lock (HashLock)
    {
      foreach (var item in HashAliases)
        if (item.Value.Alias.Equals(value)) return item.Key;

      return value;
    }
  }

  public static string GetAliasFromType(string value)
  {
    lock (HashLock)
    {
      foreach (var item in HashAliases)
        if (item.Key.Equals(value)) return item.Value.Alias;

      return CleanName(value);
    }
  }

  public static string CleanName(string name)
  {
    var result = new char[name.Length];
    var ignore = false;
    var comma = false;
    var index = 0;

    for (var i = 0; i < name.Length; i++)
    {
      var c = name[i];

      if (c == '[')
        comma = true;
      else if (c == ']')
      {
        comma = false;
        ignore = false;
      }
      else if (comma && c == ',')
        ignore = true;

      if (!ignore)
        result[index++] = c;
    }

    return new string(result, 0, index);
  }
}
