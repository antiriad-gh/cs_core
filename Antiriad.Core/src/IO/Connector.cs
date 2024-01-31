using Antiriad.Core.Log;
using Antiriad.Core.Threading;

namespace Antiriad.Core.IO;

/// <summary>
/// Base class for any data transmision medium
/// </summary>
public abstract class Connector
{
  protected volatile bool stop;
  private Thread? thread;
  protected ConnectorBuffer? buffer;
  private bool connectedNotified;

  protected IConnectorEvents? connectorListener;
  protected IConnectorDataEvents? receiverListener;

  private bool reconnect;

  protected int sendBufferSize;
  protected bool OwnDevice = true;

  protected byte[] receiveBuffer = new byte[1024 * 1024];

  public bool IsWrapped { get { return !this.OwnDevice; } }

  /// <summary>
  /// True if connection is established
  /// </summary>
  public virtual bool IsConnected { get { return false; } }
  /// <summary>
  /// Timeout in milliseconds for read operation
  /// </summary>
  public int ReadTimeout { get; set; } = 5000;
  /// <summary>
  /// Timeout in milliseconds for write operation
  /// </summary>
  public int WriteTimeout { get; set; } = 5000;
  public object? Context { get; set; }

  /// <summary>
  /// Enables auto re-connection (only applies for owned devices)
  /// </summary>
  public bool Reconnect
  {
    get => this.reconnect && this.OwnDevice;
    set => this.reconnect = value && this.OwnDevice;
  }

  /// <summary>
  /// Delegate for manage packet integrity
  /// </summary>
  public ConnectorSizer? Sizer
  {
    get => this.buffer?.Sizer;
    set => this.buffer = new ConnectorBuffer(value!);
  }

  /// <summary>
  /// Activates device for IO operations
  /// </summary>
  /// <param name="listener">Optional implementor of IConnectorEvents and/or IConnectorDataEvents</param>
  /// <returns>True if succeeded</returns>
  public virtual bool Activate(object? listener = null)
  {
    this.stop = false;
    this.connectorListener = listener as IConnectorEvents;
    this.receiverListener = listener as IConnectorDataEvents;
    return this.CreateReadThread();
  }

  protected virtual bool CreateReadThread()
  {
    var isok = this.Connect() || this.Reconnect;

    if (isok && (this.Reconnect || this.receiverListener != null))
    {
      this.thread = new Thread(this.ThreadRun);
      this.thread.Start();
    }

    return isok;
  }

  /// <summary>
  /// Deactivates device
  /// </summary>
  /// <returns>True if succeeded</returns>
  public virtual bool Deactivate()
  {
    this.stop = true;
    var isok = this.Disconnect(false);
    var thread = this.thread;

    this.thread = null;

    if (thread != null && thread.ManagedThreadId != Environment.CurrentManagedThreadId)
    {
      try
      {
        thread.Join(5000);
      }
      catch (ThreadAbortException) { }

      /*if (t.IsAlive)
      {
        try
        {
          t.Abort();
        }
        catch (Exception ex)
        {
          Trace.Exception(ex);
        }
      }*/
    }

    this.connectorListener = null;
    this.receiverListener = null;
    return isok;
  }

  private void ThreadRun()
  {
    if (this.ReceiveBufferSize < 1)
      this.ReceiveBufferSize = 1024 * 1024;

    do
    {
      try
      {
        if (!this.IsConnected)
        {
          this.Disconnect(true);
          if (!this.Reconnect) break;
          this.Connect();
        }

        var data = this.Read(Timeout.Infinite);

        if (!data.IsEmpty)
          this.FireReceiveData(data);
        else
          Thread.Sleep(10);
      }
      catch (ThreadAbortException)
      {
        this.stop = true;
      }
      catch (Exception ex)
      {
        Trace.Exception(ex);
        this.Disconnect(!this.stop);
        this.stop |= !this.Reconnect;
      }
    }
    while (!this.stop);
  }

  protected virtual void FireReceiveData(ReadOnlySpan<byte> buffer)
  {
    if (this.receiverListener == null)
      return;

    try
    {
      this.receiverListener.DataReceived(this, buffer);
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  /// <summary>
  /// Internal device connect (when apply)
  /// </summary>
  /// <returns></returns>
  protected bool Connect()
  {
    var isok = this.IsConnected;

    if (isok)
    {
      this.FireConnected();
      return true;
    }

    try
    {
      this.DeviceConnect();
      isok = this.IsConnected;
      if (isok) this.FireConnected();
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }

    return isok;
  }

  /// <summary>
  /// Internal device disconnect (when apply)
  /// </summary>
  /// <param name="lost"></param>
  /// <returns></returns>
  protected bool Disconnect(bool lost)
  {
    var isok = false;

    try
    {
      this.DeviceDisconnect();
      this.FireDisconnected(lost);
      isok = !this.IsConnected;
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }

    return isok;
  }

  /// <summary>
  /// Gets/Sets receive buffer size
  /// </summary>
  public int ReceiveBufferSize
  {
    get { return this.receiveBuffer.Length; }
    set
    {
      this.receiveBuffer = new byte[value];
      this.CheckBufferSize();
    }
  }

  /// <summary>
  /// Gets/Sets send buffer size
  /// </summary>
  public int SendBufferSize
  {
    get { return this.sendBufferSize; }
    set
    {
      this.sendBufferSize = value;
      this.CheckBufferSize();
    }
  }

  protected virtual void CheckBufferSize() { }

  protected virtual int GetReceiveBufferSize()
  {
    return this.receiveBuffer.Length;
  }

  protected virtual int GetSendBufferSize()
  {
    return 0;
  }

  protected virtual void FireConnected()
  {
    if (this.connectedNotified)
      return;

    try
    {
      this.connectedNotified = true;
      ThreadRunner.Spawn(o => this.connectorListener?.ConnectionEstablished(this));
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  protected virtual void FireDisconnected(bool lost)
  {
    if (!this.connectedNotified)
      return;

    try
    {
      this.connectedNotified = false;
      this.connectorListener?.ConnectionClosed(this, lost);

      this.DeviceDisconnect();
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  public ReadOnlySpan<byte> Read(int timeout)
  {
    try
    {
      if (buffer != null)
      {
        if (this.buffer == null)
        {
          var count = this.DeviceRead(this.receiveBuffer, 0, this.receiveBuffer.Length, timeout);
          return this.receiveBuffer.AsSpan()[..count];
        }

        var endTime = DateTime.UtcNow.AddMilliseconds(timeout);

        while ((timeout <= 0 || endTime > DateTime.UtcNow) && this.IsConnected)
        {
          var data = this.buffer.Pop();

          if (!data.IsEmpty)
            return data;

          var packetSize = this.DeviceRead(this.receiveBuffer, 0, this.receiveBuffer.Length, timeout);

          if (packetSize == 0 && timeout <= 0) // broken socket can be seen as connected and returns zero data
            break;

          this.buffer.Push(this.receiveBuffer.AsSpan()[..packetSize]);
        }
      }
    }
    catch (Exception ex)
    {
      if (ex is not System.Net.Sockets.SocketException)
        Trace.Error(ex.Message);

      this.Disconnect(true);
    }

    return ReadOnlySpan<byte>.Empty;
  }

  public int Write(ReadOnlySpan<byte> buffer)
  {
    var alreadySent = 0;

    try
    {
      if (this.sendBufferSize == 0)
      {
        this.sendBufferSize = this.GetSendBufferSize();
        if (this.sendBufferSize < 0x2000) this.sendBufferSize = 0x2000;
      }

      var count = buffer.Length;
      var offset = 0;
      var chunk = this.sendBufferSize;
      var currentCount = count;
      var bytes = buffer.ToArray();

      while (alreadySent < count && this.IsConnected)
      {
        currentCount = chunk + alreadySent > count ? count - alreadySent : chunk;
        if ((alreadySent += this.DeviceWrite(bytes, offset, currentCount)) >= count) break;
        offset += alreadySent;
      }

      this.DeviceFlush();
    }
    catch (Exception)
    {
      this.Disconnect(true);
      throw;
    }

    return alreadySent;
  }

  protected abstract void DeviceConnect();
  protected abstract void DeviceDisconnect();
  protected abstract int DeviceRead(byte[] buffer, int offset, int count, int timeout);
  protected abstract int DeviceWrite(byte[] buffer, int offset, int count);
  protected virtual void DeviceFlush() { }
}
