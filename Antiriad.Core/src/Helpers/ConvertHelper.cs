using System.Data;

namespace Antiriad.Core.Helpers;

/// <summary>
/// Converter helper
/// </summary>
public static class ConvertHelper
{
  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="value"></param>
  /// <param name="defvalue"></param>
  /// <returns></returns>
  public static T? AsType<T>(object value, T defvalue)
  {
    var t = typeof(T);

    if (t == typeof(ValueType))
      return Typer.To<T>(value);

    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
      t = Nullable.GetUnderlyingType(t);

    if (t!.IsEnum) t = typeof(int);
    return value != DBNull.Value && value != null ? (T?)Typer.Cast(t, value) : t == Typer.TypeString ? (T)(object)string.Empty : defvalue;
  }

  /// <summary>
  /// Converts commons text strings to boolean type
  /// </summary>
  /// <param name="srcval">Lowercased literals:
  /// true, 1, enable, enabled, yes, available, on
  /// </param>
  /// <returns></returns>
  public static bool BoolFromStr(string srcval)
  {
    if (!string.IsNullOrEmpty(srcval))
      switch (srcval.ToLower())
      {
        case "true":
        case "1":
        case "enable":
        case "enabled":
        case "yes":
        case "available":
        case "on":
          return true;
      }

    return false;
  }

  private static T? GetFieldValue<T>(IDataRecord reader, string field, T defvalue, bool raise)
  {
    try
    {
      for (var i = 0; i < reader.FieldCount; i++)
        if (reader.GetName(i).EqualsOrdinalIgnoreCase(field))
          return AsType(reader.GetValue(i), defvalue);
    }
    catch { }

    if (raise)
      throw new FieldAccessException($"field not found or invalid type. name={field}");

    return defvalue;
  }

  /// <summary>
  /// Returns a field value converted to given generic type with default value
  /// </summary>
  /// <typeparam name="T">Type of desired value infered from defvalue</typeparam>
  /// <param name="reader">Record containing field</param>
  /// <param name="field">Field Name</param>
  /// <param name="defvalue">Default value for null or non-exist field</param>
  /// <returns></returns>
  public static T? Get<T>(this IDataRecord reader, string field, T defvalue)
  {
    return GetFieldValue(reader, field, defvalue, false);
  }

  /// <summary>
  /// Returns a field value converted to given generic type
  /// </summary>
  /// <typeparam name="T">Type of desired value</typeparam>
  /// <param name="reader">Record containing field</param>
  /// <param name="field">Field Name</param>
  /// <returns></returns>
  public static T? Get<T>(this IDataRecord reader, string field)
  {
    return GetFieldValue(reader, field, default(T), true);
  }

  private static T? GetFieldValue<T>(DataRow row, string field, T defvalue, bool raise)
  {
    try
    {
      if (row.Table != null)
      {
        var col = row.Table.Columns;
        for (var i = 0; i < col.Count; i++)
          if (col[i].ColumnName.EqualsOrdinalIgnoreCase(field))
            return AsType(row[i], defvalue);
      }
      else
        return AsType(row[field], defvalue);
    }
    catch { }

    if (raise)
      throw new FieldAccessException($"field not found or invalid type. name={field}");

    return defvalue;
  }

  /// <summary>
  /// Returns a field value converted to given generic type with default value
  /// </summary>
  /// <typeparam name="T">Type of desired value infered from defvalue</typeparam>
  /// <param name="row">Record containing field</param>
  /// <param name="field">Field Name</param>
  /// <param name="defvalue">Default value for null or non-exist field</param>
  /// <returns></returns>
  public static T? Get<T>(this DataRow row, string field, T defvalue)
  {
    return GetFieldValue(row, field, defvalue, false);
  }

  /// <summary>
  /// Returns a field value converted to given generic type
  /// </summary>
  /// <typeparam name="T">Type of desired value</typeparam>
  /// <param name="row">Record containing field</param>
  /// <param name="field">Field Name</param>
  /// <returns></returns>
  public static T? Get<T>(this DataRow row, string field)
  {
    return GetFieldValue(row, field, default(T), true);
  }

  public static bool EqualsOrdinalIgnoreCase(this string s1, string s2)
  {
    return s1.Equals(s2, StringComparison.OrdinalIgnoreCase);
  }
}
