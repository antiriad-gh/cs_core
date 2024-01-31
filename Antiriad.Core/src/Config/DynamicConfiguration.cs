using System.Configuration;
using System.Xml;
using Antiriad.Core.Helpers;
using Antiriad.Core.Injector;
using Antiriad.Core.Log;

namespace Antiriad.Core.Config;

/// <summary>
/// Allows to read configuration mapping classes easily or without declaring anything
/// </summary>
[Serializable]
public class DynamicConfiguration : ConfigurationSection
{
  private class DynamicObject
  {
    public object? Value;
    public readonly string? Name;
    public List<DynamicObject>? Props;

    public DynamicObject(string? name)
    {
      this.Name = name;
    }

    public DynamicObject Add(string name)
    {
      this.Props ??= new List<DynamicObject>();
      var obj = new DynamicObject(name);
      this.Props.Add(obj);
      return obj;
    }

    public void Add(string name, object value)
    {
      this.Add(name).Value = value;
    }

    public IEnumerable<KeyValuePair<string, object>> GetDict()
    {
      return this.AddChildProp(this) as IEnumerable<KeyValuePair<string, object>> ?? new Dictionary<string, object>();
    }

    private object? AddChildProp(DynamicObject obj)
    {
      if (obj.Props == null) return obj.Value;
      var dict = new List<KeyValuePair<string, object>>();

      foreach (var prop in obj.Props)
        dict.Add(new KeyValuePair<string, object>(prop.Name!, this.AddChildProp(prop)!));

      return dict;
    }
  }

  private readonly DynamicObject root = new(null);

  protected override void DeserializeSection(XmlReader reader)
  {
    this.ReadSection(reader, this.root, true);
  }

  private void ReadSection(XmlReader reader, DynamicObject obj, bool read)
  {
    var depth = reader.Depth;
    DynamicObject? lastItem = null;

    while (true)
    {
      if (read && !reader.Read()) break;
      read = true;

      if (reader.NodeType != XmlNodeType.Element) // || reader.Depth == 0)
        continue;

      //if (reader.Depth > 1 && reader.Depth > depth)
      if (reader.Depth > depth)
      {
        this.ReadSection(reader, lastItem!, false);
        read = false;
        depth = reader.Depth;
        continue;
      }

      if (reader.Depth < depth) return;

      depth = reader.Depth;
      lastItem = obj.Add(reader.Name);
      ReadAttributes(reader, lastItem);
    }
  }

  private static void ReadAttributes(XmlReader reader, DynamicObject lastItem)
  {
    for (var i = 0; i < reader.AttributeCount; i++)
    {
      reader.MoveToAttribute(i);
      lastItem.Add(reader.Name, reader.Value);
    }
  }

  /// <summary>
  /// Retrieves configuration info in a Name + Value List
  /// </summary>
  /// <param name="name">name of the tag or path</param>
  /// <returns></returns>
  public object? Get(string name)
  {
    var sections = name.Split('/');
    var nodes = this.root?.Props?.FirstOrDefault();

    if (nodes != null)
      for (var i = 0; i < sections.Length; i++)
      {
        var found = nodes.Props!.Find(s => s.Name!.EqualsOrdinalIgnoreCase(sections[i]));
        if (found == null) break;
        if (i == sections.Length - 1) return found.Props != null ? found.GetDict() : found.Value;
        nodes = found;
      }

    return null;
  }

  /// <summary>
  /// Retrieves configuration info mapped on a given class
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="name">name of the tag or path</param>
  /// <returns></returns>
  public T? Get<T>(string name) where T : new()
  {
    return this.Get(name, default(T));
  }

  /// <summary>
  /// Retrieves configuration info mapped on a given class
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="name">name of the tag or path</param>
  /// <param name="defaultValue">default value</param>
  /// <returns></returns>
  public T? Get<T>(string name, T defaultValue)
  {
    var node = this.Get(name);
    var targetType = typeof(T);
    if ((targetType.IsValueType || targetType == Typer.TypeString) && node != null) return Typer.To<T>(node);
    return node is IEnumerable<KeyValuePair<string, object>> dict ? (T)dict.Map(targetType) : defaultValue;
  }

  private IEnumerable<DynamicObject>? DoGetList(string name)
  {
    var sections = name.Split('/');
    var nodes = this.root.Props;

    for (var i = 0; nodes != null && i < sections.Length; i++)
    {
      var list = nodes.Where(s => s.Name!.EqualsOrdinalIgnoreCase(sections[i])).ToList();
      if (i < sections.Length - 1) nodes = list.SelectMany(s => s.Props!).ToList();
    }

    return nodes;
  }

  /// <summary>
  /// Retrieves configuration info in a list containting Name + Value List
  /// </summary>
  /// <param name="name">name of the tag or path</param>
  /// <returns></returns>
  public IEnumerable<KeyValuePair<string, object>>? GetList(string name)
  {
    return this.DoGetList(name)?.SelectMany(i => i.GetDict());
  }

  /// <summary>
  /// Retrieves configuration info mapped in a list of given class
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="name">name of the tag or path</param>
  /// <returns></returns>
  public IEnumerable<T>? GetList<T>(string name) where T : new()
  {
    return this.DoGetList(name)?.Select(i => i.GetDict().Map<T>());
  }

  /// <summary>
  /// Gets the section instance
  /// </summary>
  /// <param name="section"></param>
  /// <returns></returns>
  public static DynamicConfiguration GetSection(string section)
  {
    if (ConfigurationManager.GetSection(section) is DynamicConfiguration config)
      return config;

    Trace.Debug($"creating blank config for section={section}");
    return new DynamicConfiguration();
  }
}
