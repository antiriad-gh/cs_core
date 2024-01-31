/*using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Antiriad.Core.IO
{
  [Serializable]
  public class FileData
  {
    public readonly FileAttributes Attributes;
    public readonly DateTime LastWriteTimeUtc;
    public readonly long Size;
    public readonly string Name;
    public readonly string Path;

    public DateTime CreationTime
    {
      get { return this.CreationTimeUtc.ToLocalTime(); }
    }

    public readonly DateTime CreationTimeUtc;

    public DateTime LastAccesTime
    {
      get { return this.LastAccessTimeUtc.ToLocalTime(); }
    }

    public readonly DateTime LastAccessTimeUtc;

    public DateTime LastWriteTime
    {
      get { return this.LastWriteTimeUtc.ToLocalTime(); }
    }

    public override string ToString()
    {
      return this.Name;
    }

    internal FileData(string dir, WIN32_FIND_DATA findData)
    {
      this.Attributes = findData.dwFileAttributes;
      this.CreationTimeUtc = ConvertDateTime(findData.ftCreationTime_dwHighDateTime, findData.ftCreationTime_dwLowDateTime);
      this.LastAccessTimeUtc = ConvertDateTime(findData.ftLastAccessTime_dwHighDateTime, findData.ftLastAccessTime_dwLowDateTime);
      this.LastWriteTimeUtc = ConvertDateTime(findData.ftLastWriteTime_dwHighDateTime, findData.ftLastWriteTime_dwLowDateTime);
      this.Size = CombineHighLowInts(findData.nFileSizeHigh, findData.nFileSizeLow);
      this.Name = findData.cFileName;
      this.Path = System.IO.Path.Combine(dir, findData.cFileName);
    }

    private static long CombineHighLowInts(uint high, uint low)
    {
      return (((long)high) << 0x20) | low;
    }

    private static DateTime ConvertDateTime(uint high, uint low)
    {
      var fileTime = CombineHighLowInts(high, low);
      return DateTime.FromFileTimeUtc(fileTime);
    }
  }

  [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto), BestFitMapping(false)]
  internal class WIN32_FIND_DATA
  {
    public FileAttributes dwFileAttributes;
    public uint ftCreationTime_dwLowDateTime;
    public uint ftCreationTime_dwHighDateTime;
    public uint ftLastAccessTime_dwLowDateTime;
    public uint ftLastAccessTime_dwHighDateTime;
    public uint ftLastWriteTime_dwLowDateTime;
    public uint ftLastWriteTime_dwHighDateTime;
    public uint nFileSizeHigh;
    public uint nFileSizeLow;
    public int dwReserved0;
    public int dwReserved1;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string cFileName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    public string cAlternateFileName;

    public override string ToString()
    {
      return "File name=" + cFileName;
    }
  }

  public static class Directory
  {
    public static IEnumerable<FileData> EnumerateFiles(string path)
    {
      return Directory.EnumerateFiles(path, "*");
    }

    public static IEnumerable<FileData> EnumerateFiles(string path, string searchPattern)
    {
      return Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
    }

    public static IEnumerable<FileData> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
    {
      if (path == null)
        throw new ArgumentNullException("path");

      if (searchPattern == null)
        throw new ArgumentNullException("searchPattern");

      if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
        throw new ArgumentOutOfRangeException("searchOption");

      return new FileEnumerable(Path.GetFullPath(path), searchPattern, searchOption);
    }

    public static FileData[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
      var e = Directory.EnumerateFiles(path, searchPattern, searchOption);
      var list = new List<FileData>(e);
      var retval = new FileData[list.Count];

      list.CopyTo(retval);
      return retval;
    }

    private class FileEnumerable : IEnumerable<FileData>
    {
      private readonly string path;
      private readonly string filter;
      private readonly SearchOption searchOption;

      public FileEnumerable(string path, string filter, SearchOption searchOption)
      {
        this.path = path;
        this.filter = filter;
        this.searchOption = searchOption;
      }

      public IEnumerator<FileData> GetEnumerator()
      {
        return new FileEnumerator(this.path, this.filter, this.searchOption);
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
        return new FileEnumerator(this.path, this.filter, this.searchOption);
      }
    }

    private sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
      [DllImport("kernel32.dll")]
      private static extern bool FindClose(IntPtr handle);

      [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
      internal SafeFindHandle()
        : base(true)
      {
      }

      protected override bool ReleaseHandle()
      {
        return FindClose(base.handle);
      }
    }

    [System.Security.SuppressUnmanagedCodeSecurity]
    private class FileEnumerator : IEnumerator<FileData>
    {
      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern SafeFindHandle FindFirstFile(string fileName, [In, Out] WIN32_FIND_DATA data);

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool FindNextFile(SafeFindHandle hndFindFile, [In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_DATA lpFindFileData);

      private class SearchContext
      {
        public readonly string Path;
        public Stack<string> SubdirectoriesToProcess;

        public SearchContext(string path)
        {
          this.Path = path;
        }
      }

      private string path;
      private readonly string filter;
      private readonly SearchOption searchOption;
      private readonly Stack<SearchContext> contextStack;
      private SearchContext currentContext;

      private SafeFindHandle hndFindFile;
      private readonly WIN32_FIND_DATA win_find_data = new WIN32_FIND_DATA();

      public FileEnumerator(string path, string filter, SearchOption searchOption)
      {
        this.path = path;
        this.filter = filter;
        this.searchOption = searchOption;
        this.currentContext = new SearchContext(path);

        if (this.searchOption == SearchOption.AllDirectories)
          this.contextStack = new Stack<SearchContext>();
      }

      public FileData Current
      {
        get { return new FileData(this.path, this.win_find_data); }
      }

      public void Dispose()
      {
        if (this.hndFindFile != null)
        {
          this.hndFindFile.Dispose();
        }
      }

      object System.Collections.IEnumerator.Current
      {
        get { return new FileData(this.path, this.win_find_data); }
      }

      public bool MoveNext()
      {
        var retval = false;

        if (this.currentContext.SubdirectoriesToProcess == null)
        {
          if (this.hndFindFile == null)
          {
            //new FileIOPermission(FileIOPermissionAccess.PathDiscovery, this.path).Demand();

            var searchPath = Path.Combine(this.path, this.filter);
            this.hndFindFile = FindFirstFile(searchPath, this.win_find_data);
            retval = !this.hndFindFile.IsInvalid;
          }
          else
            retval = FindNextFile(this.hndFindFile, this.win_find_data);
        }

        if (retval)
        {
          if ((this.win_find_data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            return MoveNext();
        }
        else if (this.searchOption == SearchOption.AllDirectories)
        {
          if (this.currentContext.SubdirectoriesToProcess == null)
          {
            var subDirectories = System.IO.Directory.GetDirectories(this.path);
            this.currentContext.SubdirectoriesToProcess = new Stack<string>(subDirectories);
          }

          if (this.currentContext.SubdirectoriesToProcess.Count > 0)
          {
            var subDir = this.currentContext.SubdirectoriesToProcess.Pop();

            this.contextStack.Push(this.currentContext);
            this.path = subDir;
            this.hndFindFile = null;
            this.currentContext = new SearchContext(this.path);

            return this.MoveNext();
          }

          if (this.contextStack.Count > 0)
          {
            this.currentContext = this.contextStack.Pop();
            this.path = this.currentContext.Path;

            if (this.hndFindFile != null)
            {
              this.hndFindFile.Close();
              this.hndFindFile = null;
            }

            return this.MoveNext();
          }
        }

        return retval;
      }

      public void Reset()
      {
        this.hndFindFile = null;
      }
    }
  }
}
*/