namespace Antiriad.Core.Log;

public class LoggerConfig
{
  public string Path = string.Empty;
  public string FileName = string.Empty;
  public int FileCount;
  public int FileSize;
  public LogLevel LogLevel;
  public bool Enabled;
  public bool ConsoleOutput;
  public bool Append;
}
