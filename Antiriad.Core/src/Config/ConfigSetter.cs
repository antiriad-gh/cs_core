using System.Reflection;

namespace Antiriad.Core.Config;

internal class ConfigSetter
{
  private readonly object instance;
  private readonly PropertyInfo? prop;
  private readonly FieldInfo? field;

  public ConfigSetter(object instance, PropertyInfo prop)
  {
    this.instance = instance;
    this.prop = prop;
  }

  public ConfigSetter(object instance, FieldInfo field)
  {
    this.instance = instance;
    this.field = field;
  }

  public Type MemberType => this.prop?.PropertyType! ?? this.field?.FieldType!;

  internal void SetValue(object? value)
  {
    if (this.field != null)
      this.field.SetValue(this.instance, value);
    else
      this.prop?.SetValue(this.instance, value);
  }
}
