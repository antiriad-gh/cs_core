using System.Security;
using Antiriad.Core.Threading;

namespace Antiriad.Core.Log;

internal class Logger
{
    internal readonly LogConfiguration Config;
    private StreamWriter? file;
    private DateTime nextWrite = DateTime.MinValue;
    private bool finished;
    private int callerPadding = 20;
    private static readonly object locker = new();
    private volatile bool spawnRunning;
    private readonly Queue<(DateTime, LogLevel, string)> queue = new();

    public Logger(LogConfiguration conf)
    {
        this.Config = conf;
        this.Debug("[LOG-STARTED]");

        AppDomain.CurrentDomain.ProcessExit += this.OnMainThreadExit;
        AppDomain.CurrentDomain.UnhandledException += this.OnUnhandledException;
        AppDomain.CurrentDomain.AssemblyLoad += this.OnAssemblyLoad;
    }

    private void OnMainThreadExit(object? sender, EventArgs eventArgs)
    {
        this.Debug("[LOG-STOPPED]");
        this.Close(true);
    }

    [SecurityCritical]
    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs eventArgs)
    {
        if (eventArgs.ExceptionObject is Exception exception)
        {
            Trace.Exception(exception);
        }

        this.Close(true);
    }

    private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        try
        {
            var location = args.LoadedAssembly.IsDynamic ? "N/A" : args.LoadedAssembly.Location;
            this.Message($"[ASSEMBLY-LOADED] FullName={args.LoadedAssembly.FullName} - Location={location}");
        }
        catch
        {
            System.Diagnostics.Trace.WriteLine("[LOGGER] Failed to handle AssemblyLoad event to log the assembly being loaded.");
        }
    }

    public void Debug(string fmt, string caller = "")
    {
        this.LogText(LogLevel.Debug, fmt, caller);
    }

    public void Error(string fmt, string caller = "")
    {
        this.LogText(LogLevel.Error, fmt, caller);
    }

    public void Warning(string fmt, string caller = "")
    {
        this.LogText(LogLevel.Warning, fmt, caller);
    }

    public void Message(string fmt, string caller = "")
    {
        this.LogText(LogLevel.Trace, fmt, caller);
    }

    public void Exception(Exception e, string caller = "")
    {
        this.LogText(LogLevel.Error, e.ToString(), caller);
    }

    private void LogText(LogLevel level, string text, string caller = "")
    {
        if (this.Config is null || this.Config.LogLevel == LogLevel.None || this.finished)
            return;

        lock (Logger.locker)
        {
            if (this.file is null || this.file.BaseStream.Length >= this.Config.MaxFileSize)
                this.OpenFile();

            var levelChar = level switch
            {
                LogLevel.Debug => "-",
                LogLevel.Error => "@",
                LogLevel.Warning => "!",
                _ => "="
            };

            if (caller.Length > this.callerPadding)
                this.callerPadding = caller.Length + 2;

            var now = DateTime.Now;
            var threadId = Environment.CurrentManagedThreadId;
            var callerfmt = caller.PadRight(this.callerPadding);
            var textLine = $"{levelChar} {now.Year:d4}-{now.Month:d2}-{now.Day:d2} {now.Hour:d2}:{now.Minute:d2}:{now.Second:d2}.{now.Millisecond:d3} [{threadId:x6}] {callerfmt}| {text}";

            if (this.Config.AsyncWrite)
            {
                lock (this.queue)
                {
                    this.queue.Enqueue((now, level, textLine));

                    if (!this.spawnRunning)
                    {
                        this.spawnRunning = ThreadRunner.Spawn(this.DequeueDispatch);
                    }
                }
            }
            else
            {
                this.WriteLine(now, level, textLine);
            }
        }
    }

    private void DequeueDispatch()
    {
        while (true)
        {
            (DateTime Now, LogLevel Level, string TextLine) item;

            lock (this.queue!)
            {
                if (!this.queue.TryDequeue(out item))
                {
                    this.spawnRunning = false;
                    return;
                }
            }

            this.WriteLine(item.Now, item.Level, item.TextLine);
        }
    }

    private void WriteLine(DateTime now, LogLevel level, string textLine)
    {
        if (this.Config.ConsoleOutput)
        {
            Console.ForegroundColor = level switch
            {
                LogLevel.Debug or LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            Console.WriteLine(textLine);
        }

        this.file!.WriteLine(textLine);

        if (this.Config.FlushInterval == 0 || now >= this.nextWrite)
        {
            this.file.Flush();
            this.nextWrite = now.AddMilliseconds(this.Config.FlushInterval);
        }
    }

    private void Close(bool finished)
    {
        lock (locker)
        {
            if (this.file != null)
            {
                this.file.Flush();
                this.file.Close();
                this.file = null;
            }

            this.finished = finished;
        }
    }

    private void OpenFile()
    {
        this.Close(false);

        if (!Directory.Exists(this.Config.Path))
            Directory.CreateDirectory(this.Config.Path);

        if (!Directory.Exists(this.Config.Path))
            return;

        var boff = 0;
        var genname = this.RotateFiles();
        var name = genname;

        while (true)
        {
            try
            {
                this.file = File.AppendText(name);
                if (this.file.BaseStream.Length > 0)
                    this.file.WriteLine();
                break;
            }
            catch (Exception)
            {
                if (File.Exists(name))
                {
                    if (boff > 99)
                        break;

                    boff++;
                    name = $"{genname}-{boff:D2}";
                }
            }
        }
    }

    private string RotateFiles()
    {
        var name = Path.Combine(this.Config.Path, $"{this.Config.FileName}.log");

        if (!File.Exists(name) || (this.Config.Append && new FileInfo(name).Length < this.Config.MaxFileSize))
        {
            return name;
        }

        var files = Directory.GetFiles(this.Config.Path, Path.GetFileNameWithoutExtension(this.Config.FileName) + "*-*.log");

        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        if (files.Length > 0 && files.Length + 1 > this.Config.MaxFileCount)
        {
            for (var i = 0; i <= files.Length - this.Config.MaxFileCount; i++)
                try
                {
                    File.Delete(files[i]);
                }
                catch { }
        }

        var now = DateTime.Now;
        var date = $"{now.Year:d4}{now.Month:d2}{now.Day:d2}";
        var time = $"{now.Hour:d2}{now.Minute:d2}{now.Second:d2}";
        var appendName = Path.Combine(this.Config.Path, $"{this.Config.FileName}-{date}-{time}.log");
        var counter = 0;

        while (File.Exists(appendName) && counter <= 9999)
        {
            appendName = Path.Combine(this.Config.Path, $"{this.Config.FileName}-{date}-{time}({++counter:d4}).log");
        }

        if (!string.IsNullOrEmpty(appendName))
            try
            {
                File.Move(name, appendName);
            }
            catch { }

        return name;
    }
}
