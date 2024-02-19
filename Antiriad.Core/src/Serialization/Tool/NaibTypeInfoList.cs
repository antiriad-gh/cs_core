using System.Reflection;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Serialization.Tool;

public class NaibTypeInfoList
{
  private static readonly object AliasesLock = new();
  private static readonly Dictionary<string, string> Aliases = new();

  public static void AddAlias(string name, string alias)
  {
    lock (AliasesLock)
    {
      if (!Aliases.ContainsKey(name))
        Aliases.Add(name, alias);
    }
  }

  public static string ExpandAlias(string name)
  {
    lock (AliasesLock)
    {
      return Aliases.TryGetValue(name, out var alias) ? alias : name;
    }
  }

  private readonly object locker = new();
  private readonly List<NaibTypeInfo> cache = new();

  internal NaibTypeInfo? Find(short remoteid)
  {
    lock (this.locker) return this.cache.Find(i => i.RemoteId == remoteid);
  }

  internal NaibTypeInfo? Find(Type? type)
  {
    lock (this.locker) return this.cache.Find(i => i.Type == type);
  }

  internal NaibTypeInfo? FindOrCreate(string alias, short remoteid)
  {
    lock (this.locker)
    {
      var name = NaibTypeInfo.GetTypeFromAlias(alias);

      foreach (var i in this.cache.Where(e => e.Name.EqualsOrdinalIgnoreCase(name)))
      {
        i.RemoteId = remoteid;
        return i;
      }

      var find = name.Replace('$', '+');
      var type = Type.GetType(find) ?? SafeGetAssemblies(find);
      var info = this.Find(type);

      if (info == null)
      {
        if (type == null) return null;
        info = new NaibTypeInfo(type, -1, remoteid);
        this.cache.Add(info);
      }
      else if (info.RemoteId == -1)
        info.RemoteId = remoteid;

      return info;
    }
  }

  private static Type? SafeGetAssemblies(string find)
  {
    find = ExpandAlias(find);
    return
      AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetModules())
        .SelectMany(SafeGetTypes)
        .FirstOrDefault(t => t.FullName!.EqualsOrdinalIgnoreCase(find)) ??
        FindAssembly(find);
  }

  private static IEnumerable<Type> SafeGetTypes(Module module)
  {
    try
    {
      return module.GetTypes();
    }
    catch
    {
      return Array.Empty<Type>();
    }
  }

  private static Type? FindAssembly(string name)
  {
    var last = 0;
    while (last <= name.Length)
    {
      var asm = name.IndexOf('.', last);
      if (asm < 1) break;
      var type = Type.GetType($"{name}, {name[..asm]}");
      if (type != null) return type;
      last = asm + 1;
    }
    return null;
  }

  internal void GetId(NaibTypeInfo info)
  {
    lock (this.locker) info.LocalId = this.cache.Count == 0 ? (short)1 : (short)(this.cache.Max(i => i.LocalId) + 1);
  }

  internal NaibTypeInfo Store(Type type)
  {
    NaibTypeInfo? info;

    lock (this.locker)
    {
      info = this.Find(type);

      if (info == null)
      {
        info = new NaibTypeInfo(type, -1, -1);
        this.cache.Add(info);
      }
    }

    return info;
  }

  public void Clear()
  {
    lock (this.locker) this.cache.Clear();
  }
}
