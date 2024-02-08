using System.Runtime.CompilerServices;

using Antiriad.Core.Config;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Log;

public sealed class Trace
{
  private static readonly object locker = new();
  private static Logger? logger;
  public static string? ApplicationPath { get; private set; }

  private Trace() { }

  private static Logger Initialize()
  {
    lock (locker)
    {
      if (logger is null)
      {
        LogConfiguration conf;

        try
        {
          conf = Configuration.Read<LogConfiguration>("Log");
          conf ??= new LogConfiguration();
        }
        catch (Exception e)
        {
          conf = new LogConfiguration();
          System.Diagnostics.Trace.WriteLine($@"Trace() failed with msg '{e}'");
        }

        Configure(conf);
      }

      if (logger == null)
        throw new Exception("Logger cannot be configured");
    }

    return logger;
  }

  public static void Configure(LogConfiguration conf)
  {
    lock (locker)
    {
      var pathsep = Path.DirectorySeparatorChar;
      var rootCurrent = $".{pathsep}";
      var oldsep = pathsep == '/' ? "\\" : "/";
      var newsep = pathsep == '/' ? "/" : "\\";

      conf.Path = conf.Path.Replace(oldsep, newsep);
      ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;

      if (conf.Path.StartsWith(rootCurrent))
      {
        conf.Path = Path.Combine(ApplicationPath, conf.Path[rootCurrent.Length..]);
      }

      conf.MaxFileSize = int.Min(conf.MaxFileSize * 1024, 10 * 1_048_576);
      logger = new Logger(conf);
    }
  }

  public static LogConfiguration GetConfig() => (logger ?? Initialize()).Config;

  public static void Debug(string fmt, [CallerMemberName] string caller = "")
  {
    (logger ?? Initialize()).Debug(fmt, caller);
  }

  public static void Exception(Exception e, [CallerMemberName] string caller = "")
  {
    (logger ?? Initialize()).Exception(e, caller);
  }

  public static void Error(string fmt, [CallerMemberName] string caller = "")
  {
    (logger ?? Initialize()).Error(fmt, caller);
  }

  public static void Warning(string fmt, [CallerMemberName] string caller = "")
  {
    (logger ?? Initialize()).Warning(fmt, caller);
  }

  public static void Message(string fmt, [CallerMemberName] string caller = "")
  {
    (logger ?? Initialize()).Message(fmt, caller);
  }

  public static object? ToString(object? value)
  {
    return value is Array ? $"[{string.Join(",", Typer.To<string[]>(value)!)}]" : Typer.To<string>(value);
  }
}
