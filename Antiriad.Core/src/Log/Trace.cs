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
        LogConfiguration fileconf;
        ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;

        try
        {
          fileconf = Configuration.Read<LogConfiguration>("Log");
          fileconf ??= new LogConfiguration();
        }
        catch (Exception e)
        {
          fileconf = new LogConfiguration();
          System.Diagnostics.Trace.WriteLine($@"Trace() failed with msg '{e}'");
        }

        var pathsep = Path.DirectorySeparatorChar;
        var rootCurrent = $".{pathsep}";
        var oldsep = pathsep == '/' ? "\\" : "/";
        var newsep = pathsep == '/' ? "/" : "\\";

        fileconf.Path = fileconf.Path.Replace(oldsep, newsep);

        if (fileconf.Path.StartsWith(rootCurrent))
        {
          fileconf.Path = Path.Combine(ApplicationPath, fileconf.Path[rootCurrent.Length..]);
        }

        fileconf.MaxFileSize = int.Min(fileconf.MaxFileSize * 1024, 10 * 1_048_576);
        logger = new Logger(fileconf);
      }
    }

    return logger;
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
