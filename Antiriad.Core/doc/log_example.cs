using Antiriad.Core.Log;

public class LogSample
{
  public static void Test()
  {
    // method 1 - optional: if not used Trace reads from: <executablepath/executable>.conf 
    // .conf format:
    // [Log]
    // maxFileCount = 10
    // maxFileSize = 8096
    // logLevel = Trace
    // consoleOutput = true
    // path =./ log
    LogConfiguration conf = new();  // new log configuration
    conf.Path = "/var/log";         // setup some log prop
    Trace.Configure(conf);          // configure log

    // method 2 - just use it: automatic configuration
    Trace.Debug("put this on log file");
  }
}