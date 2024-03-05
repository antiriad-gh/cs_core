namespace Antiriad.Core.Config;

public static class ConfigFile
{
  public static T Read<T>(string? section = null) where T : new()
  {
    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName);
    return Read<T>($"{path}.conf", section ?? AppDomain.CurrentDomain.FriendlyName);
  }

  public static T Read<T>(string file, string? section = null) where T : new()
  {
    if (!File.Exists(file))
      throw new Exception($"cannot read configuration file={file} section={section}");

    var reader = new ConfigFileReader(file, section);
    return reader.Get<T>();
  }
}
