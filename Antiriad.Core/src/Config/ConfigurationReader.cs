using System.Collections;
using Antiriad.Core.Collections;
using Antiriad.Core.Helpers;
using Antiriad.Core.Log;

namespace Antiriad.Core.Config;

internal class ConfigurationReader
{
  private readonly string file;
  private readonly string? section;

  public ConfigurationReader(string file, string? section)
  {
    this.file = file;
    this.section = section;
  }

  internal static object? GetConfigValue(Type? type, string value)
  {
    if (type == null)
      return null;

    if (type.IsPrimitive || type == Typer.TypeString || type == Typer.TypeTimeSpan || type == Typer.TypeDateTime || type == Typer.TypeDateTimeOffset || type == Typer.TypeTimeOnly)
      value = value.Trim(' ', '"', '/');

    return Typer.Cast(type, value, null);
  }

  internal T Get<T>() where T : new()
  {
    T conf = new();
    var lines = File.ReadLines(this.file);
    var insection = string.IsNullOrEmpty(this.section);

    foreach (var line in lines.Select(i => i.Trim()))
    {
      if (line.StartsWith('#'))
        continue;

      if (line.StartsWith('['))
      {
        if (insection)
          break;

        if (line[1..^1] == this.section)
          insection = true;
      }
      else if (insection)
        this.AssignProperty(conf, line);
    }

    if (!insection)
      Trace.Warning($"config section={this.section} was not found");

    return conf;
  }

  private void AssignProperty(object? instance, string line)
  {
    if (instance == null || string.IsNullOrEmpty(line))
      return;

    var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);

    if (parts.Length != 2)
    {
      Trace.Error($"config parameter format error line='{line}'");
      return;
    }

    try
    {
      var name = parts[0];
      var valuestr = parts[1];
      var setter = this.GetSetter(instance, name);

      if (setter != null)
      {
        var value = this.ReadValue(setter.MemberType, valuestr);
        setter.SetValue(value);
      }
      else
        Trace.Warning($"config parameter not recognized '{this.section}.{name}'");
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  private ConfigSetter? GetSetter(object instance, string name)
  {
    if (name.StartsWith('"'))
      name = name.Trim('"');

    var configType = instance.GetType();
    var prop = configType.GetProperties().Find(i => i.Name.EqualsOrdinalIgnoreCase(name));

    if (prop != null)
      return new ConfigSetter(instance, prop);

    var field = configType.GetFields().Find(i => i.Name.EqualsOrdinalIgnoreCase(name));

    if (field != null)
      return new ConfigSetter(instance, field);

    Trace.Error($"config parameter not recognized '{this.section}.{name}'");
    return null;
  }

  private object? ReadObject(bool isarray, Type? type, string valuestr)
  {
    if (type == null)
      return null;

    object? itemobj = null;
    var itemstr = string.Empty;
    var insideString = false;
    var stringChar = '"';
    var endChar = isarray ? ']' : '}';
    var gena = type.IsArray ? new[] { type.GetElementType() } : type.GetGenericArguments();
    var gent = gena.Length > 0 ? gena[0] : type;
    var listtype = gena.Length > 0 && gent != null ? typeof(List<>).MakeGenericType(new[] { gent }) : null;
    var list = listtype != null ? Activator.CreateInstance(listtype) as IList : null;
    var isprimitive = gent != null && (gent.IsPrimitive || gent == Typer.TypeString);

    for (var i = 0; i < valuestr.Length; i++)
    {
      var c = valuestr[i];

      if (insideString)
      {
        if (c == stringChar)
          insideString = false;
        else
          itemstr += c;
      }
      else if (c == '"' || c == '\'')
      {
        stringChar = c;
        insideString = true;
      }
      else if (c == '[')
      {
      }
      else if (c == '{')
      {
        if (gent != null)
        {
          itemobj = Activator.CreateInstance(gent);
          list?.Add(itemobj);
        }
        itemstr = string.Empty;
      }
      else if (c == '}')
      {
        this.AssignProperty(itemobj, itemstr);
        itemstr = string.Empty;
      }
      else if (c == ',' || c == endChar)
      {
        if (itemstr.Length > 0)
        {
          if (isprimitive)
            list?.Add(this.ReadValue(gent, itemstr));
          else
            this.AssignProperty(itemobj, itemstr);

          itemstr = string.Empty;
        }
      }
      else
        itemstr += c;
    }

    return isarray && type.IsArray ? list?.CopyToArray() : list ?? itemobj;
  }

  private object? ReadValue(Type? type, string valuestr)
  {
    if (valuestr.StartsWith('[') && valuestr.EndsWith(']'))
      return this.ReadObject(true, type, valuestr);
    if (valuestr.StartsWith('{') && valuestr.EndsWith('}'))
      return this.ReadObject(false, type, valuestr);
    else
      return GetConfigValue(type, valuestr);
  }
}
