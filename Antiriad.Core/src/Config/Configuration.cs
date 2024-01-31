using Antiriad.Core.Log;

namespace Antiriad.Core.Config;

public static class Configuration
{
  public static T Read<T>(string? section = null) where T : new()
  {
    var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName) + ".conf";
    return Read<T>(file, section ?? AppDomain.CurrentDomain.FriendlyName);
  }

  public static T Read<T>(string file, string? section = null) where T : new()
  {
    if (!File.Exists(file))
    {
      Trace.Warning($"cannot find configuration file={file}");
      return new T();
    }

    var reader = new ConfigurationReader(file, section);
    return reader.Get<T>();
  }
}
