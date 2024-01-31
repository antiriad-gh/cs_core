using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Types;

public class CachedTypeInfo
{
  //internal short LocalId;
  //internal short RemoteId;
  //public int RemotePropCount;
  public readonly Type Type;
  public readonly string Name;
  public readonly PropertyMetadata[] Props;
  public readonly ConstructorHandler Constructor;

  public CachedTypeInfo(Type type /*, short localid, short remoteid*/)
  {
    //this.LocalId = localid;
    //this.RemoteId = remoteid;
    this.Type = type;
    this.Name = CachedTypeInfo.GetCleanName(this.Type.AssemblyQualifiedName);

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
      //Trace.Exception(ex);
    }

    try
    {
      var list = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
      metap.AddRange(list.Where(i => i.CanWrite).OrderBy(i => i.Name).Select(i => new PropertyMetadata(this.Type, i)));
    }
    catch (Exception ex)
    {
      //Trace.Exception(ex);
    }

    /*for (var i = 0; i < metap.Count; i++)
    {
        metap[i].LocalId = (short)i;
    }*/

    if (type.IsArray || type.IsPrimitive || type == Typer.TypeString)
    {
      this.Constructor = () => Activator.CreateInstance(type)!;
    }
    else if (type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null)
    {
      this.Constructor = MethodGenerator.MakeConstructorHandler(type);
    }
    else
    {
      this.Constructor = () => FormatterServices.GetUninitializedObject(type);
    }

    this.Props = metap.ToArray();
  }

  private static string GetCleanName(string? name)
  {
    if (name != null)
    {
      var index1 = name.IndexOf(',', 0);
      var index2 = name.IndexOf(',', index1 + 1);
      return index2 >= 0 ? name[..index2] : name;
    }
    else
    {
      throw new Exception($"cannot get assembly name from {name}");
    }
  }

  public object NewInstance()
  {
    try
    {
      return this.Constructor();
    }
    catch (Exception ex)
    {
      //Trace.Exception(ex);
    }

    return null;
  }

  private class HashPair
  {
    internal readonly string Alias;
    internal int Id;

    internal HashPair(string alias, int id)
    {
      this.Alias = alias;
      this.Id = id;
    }
  }

  private static readonly Dictionary<string, HashPair> HashAliases = new Dictionary<string, HashPair>();
  private static readonly object HashLock = new object();

  /*public static void Register(Type type)
  {
      lock (HashLock)
      {
          foreach (var c in type.GetNestedTypes())
          {
              var ca = (RegisterType[])c.GetCustomAttributes(typeof(RegisterType), false);

              if (ca.Length > 0)
              {
                  TypeInfo.Register(c, string.IsNullOrEmpty(ca[0].Name) ? c.Name : ca[0].Name, ca[0].Id);
              }
          }
      }
  }

  public static int Register(Type type, string alias, int id)
  {
      lock (HashLock)
      {
          var name = type.FullName;
          HashPair pair;

          if (!HashAliases.TryGetValue(name, out pair))
          {
              HashAliases.Add(name, pair = new HashPair(alias, id));

              if (pair.Id == 0)
              {
                  pair.Id = NaibStream.CalculateHash(alias);
              }

              //Trace.Debug(string.Format("register class={0} alias={1} id={2}", name, alias, pair.Id));
          }

          return pair.Id;
      }
  }*/

  /*public static int GetHash(object value)
  {
      return GetHash(value.GetType());
  }

  public static int GetHash(Type type)
  {
      return NaibStream.CalculateHash(type.FullName);
  }*/

  public static string GetTypeFromAlias(string value)
  {
    lock (HashLock)
    {
      foreach (var item in HashAliases)
      {
        if (item.Value.Alias.Equals(value))
        {
          return item.Key;
        }
      }

      return value;
    }
  }

  public static string GetAliasFromType(string value)
  {
    lock (HashLock)
    {
      foreach (var item in HashAliases)
      {
        if (item.Key.Equals(value))
        {
          return item.Value.Alias;
        }
      }

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
      {
        comma = true;
      }
      else if (c == ']')
      {
        comma = false;
        ignore = false;
      }
      else if (comma && c == ',')
      {
        ignore = true;
      }

      if (!ignore)
      {
        result[index++] = c;
      }
    }

    return new string(result, 0, index);
  }
}
