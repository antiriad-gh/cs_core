using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Types;

public class CachedTypeInfo
{
  public readonly Type Type;
  public readonly string Name;
  public readonly Dictionary<string, PropertyMetadata> Props;
  public readonly ConstructorHandler Constructor;

  public CachedTypeInfo(Type type)
  {
    this.Type = type;
    this.Name = CachedTypeInfo.GetCleanName(this.Type.AssemblyQualifiedName);

    var metap = new List<PropertyMetadata>();

    try
    {
      var list = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
      metap.AddRange(list.Where(i => i.GetCustomAttributes(typeof(CompilerGeneratedAttribute), true).Length == 0).OrderBy(i => i.Name).Select(i => new PropertyMetadata(this.Type, i)));
    }
    catch (Exception)
    {
    }

    try
    {
      var list = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
      metap.AddRange(list.Where(i => i.CanWrite).OrderBy(i => i.Name).Select(i => new PropertyMetadata(this.Type, i)));
    }
    catch (Exception)
    {
    }

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

    this.Props = metap.ToDictionary(i => i.Name, i => i);
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

  public object? NewInstance()
  {
    try
    {
      return this.Constructor();
    }
    catch (Exception)
    {
    }

    return null;
  }
}
