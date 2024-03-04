namespace Antiriad.Core.Log;

public class LogConfiguration
{
  public LogConfiguration()
  {
    this.Path = GetProcessDirectory();
    this.FileName = AppDomain.CurrentDomain.FriendlyName;
    this.LogLevel = LogLevel.Debug;
  }

  public string Path { get; set; }

  public int MaxFileSize { get; set; } = 4096;

  public int MaxFileCount { get; set; } = 10;
  public int FlushIntervalMM { get; set; } = 500;

  public bool Append { get; set; }

  public string FileName { get; set; }

  public LogLevel LogLevel { get; set; }

  public bool ConsoleOutput { get; set; } = true;

  internal static string GetProcessDirectory()
  {
    var loc = System.Reflection.Assembly.GetEntryAssembly()?.Location;
    var dn = System.IO.Path.GetDirectoryName(loc);
    return dn ?? string.Empty;
  }
}
