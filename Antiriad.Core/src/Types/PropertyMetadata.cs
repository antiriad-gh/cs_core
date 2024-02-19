using System.Reflection;

namespace Antiriad.Core.Types;

public class PropertyMetadata
{
  private readonly MemberMetadata cached;

  public string Name { get { return this.cached.Info.Name; } }
  public Type DataType { get { return this.cached.DataType; } }
  internal GetHandler Getter { get { return this.cached.Getter; } }
  internal SetHandler Setter { get { return this.cached.Setter; } }

  public short LocalId = -1;
  public short RemoteId = -1;

  public PropertyMetadata(Type type, PropertyInfo info)
  {
    this.cached = MemberMetadata.Get(type, info);
  }

  public PropertyMetadata(Type type, FieldInfo info)
  {
    this.cached = MemberMetadata.Get(type, info);
  }

  public void Set(object source, object? value)
  {
    this.Setter?.Invoke(source, value);
  }

  public object? Get(object source)
  {
    return this.Getter?.Invoke(source);
  }
}
