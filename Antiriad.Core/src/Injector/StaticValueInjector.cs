using System.Collections;
using Antiriad.Core.Collections;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Injector;

public static class StaticValueInjector
{
  /// <summary>
  /// Inject values from Dictionary
  /// </summary>
  /// <param name="target">the target where the value is going to be injected</param>
  /// <param name="source">source from where the value is taken</param>
  /// <param name="comparison">the method for comparing property name</param>
  /// <returns>the modified target</returns>
  public static object InjectFrom(this object target, IEnumerable<KeyValuePair<string, object>> source, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
  {
    var props = target.GetType().GetProperties();

    foreach (var value in source)
    {
      var k = value.Key;
      var prop = props.Find(i => i.Name.Equals(k, comparison));

      if (prop == null)
      {
        prop = props.Find(i => i.PropertyType.IsAssignableFrom(typeof(IDictionary<string, object>)));

        if (prop == null)
          continue;

        var pval = prop.GetValue(target, null);
        var dict = pval as ICollection<KeyValuePair<string, object>> ?? new Dictionary<string, object>();

        if (!dict.Any(i => i.Key.Equals(k, comparison)))
          dict.Add(new KeyValuePair<string, object>(k, value.Value));

        if (pval == null && prop.CanWrite)
          prop.SetValue(target, dict, null);

        continue;
      }

      object sub;

      if (value.Value is not IEnumerable<KeyValuePair<string, object>> dat)
      {
        prop.SetValue(target, Typer.Cast(prop.PropertyType, value.Value), null);
        continue;
      }

      if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
      {
        var ga = prop.PropertyType.GetGenericArguments();
        if (ga.Length != 1) continue;

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(ga));

        foreach (var pair in dat)
        {
          sub = Activator.CreateInstance(ga[0]);

          if (pair.Value is IEnumerable<KeyValuePair<string, object>> subprop)
            sub.InjectFrom(subprop, comparison);

          list.Add(sub);
        }

        prop.SetValue(target, list, null);
        continue;
      }

      sub = Activator.CreateInstance(prop.PropertyType);
      sub.InjectFrom(dat, comparison);
      prop.SetValue(target, sub, null);
    }

    return target;
  }

  /// <summary>
  /// Creates object <code>T</code> and assigns property values from source.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="source"></param>
  /// <param name="comparison"></param>
  /// <returns></returns>
  public static T Map<T>(this IEnumerable<KeyValuePair<string, object>> source, StringComparison comparison = StringComparison.OrdinalIgnoreCase) where T : new()
  {
    return (T)InjectFrom(new T(), source, comparison);
  }

  public static object Map(this IEnumerable<KeyValuePair<string, object>> source, Type type, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
  {
    return InjectFrom(Activator.CreateInstance(type), source, comparison);
  }
}
