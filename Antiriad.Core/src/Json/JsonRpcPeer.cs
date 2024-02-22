using System.Net.Sockets;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

using Antiriad.Core.IO;
using Antiriad.Core.Log;
using Antiriad.Core.Threading;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Json;

/// <summary>
/// Client for JSON-RPC connections
/// </summary>
public class JsonRpcPeer : IDisposable, IConnectorDataEvents
{
  private static readonly string[] Nologkeys = { "keepalive", "watchdog" };

  private readonly object idLock = new();
  private int session;
  private int id;

  /// <summary>
  /// Creates a JSON-RPC client with async event method
  /// </summary>
  public JsonRpcPeer() : this(true) { }

  /// <summary>
  /// Creates a JSON-RPC client
  /// </summary>
  /// <param name="async">True for async event method</param>
  public JsonRpcPeer(bool async)
  {
    this.binder = new JsonMethodBinder(typeof(JsonRpcPacket<>), async);
  }

  public IJsonRpcPacket<T> CreateResponse<T>(IJsonRpcPacket packet) where T : class, new()
  {
    return new JsonRpcResponse<T> { Id = packet.Id, Session = this.session, Data = new T() };
  }

  public IJsonRpcPacket<T> CreateResponse<T>() where T : class, new()
  {
    return new JsonRpcResponse<T> { Id = this.binder.PacketInfo.Value.Id, Session = this.session, Data = new T() };
  }

  public IJsonRpcPacket<T> CreateNotification<T>() where T : class, new()
  {
    return new JsonRpcPacket<T> { Method = JsonMethodBinder.GetMethodName(typeof(T)), Session = this.session, Data = new T() };
  }

  public IJsonRpcPacket<T> CreateRequest<T>(T data = null) where T : class, new()
  {
    return new JsonRpcPacket<T> { Method = JsonMethodBinder.GetMethodName(typeof(T)), Session = this.session, Id = this.GetNextId(), Data = data };
  }

  private int GetNextId()
  {
    lock (this.idLock)
    {
      return this.id >= int.MaxValue ? (this.id = 1) : ++this.id;
    }
  }

  private readonly object sendLock = new();

  private Connector connector;
  private readonly JsonMethodBinder binder;
  private readonly JsonRpcPacketCompleter bufr = new();
  private bool closeNotified;
  private int dumpSize = 200;
  private int receiveTimeout = 10000;

  private int receiveBufferSize = 1024 * 1024;
  private int sendBufferSize = 1024 * 1024;

  public int DumpSize { get { return this.dumpSize == 0 ? int.MaxValue : this.dumpSize; } set { this.dumpSize = value; } }
  public bool Dump { get; set; }
  public int ReceiveTimeout { get { return this.receiveTimeout; } set { this.receiveTimeout = value; } }
  public string Code { get; set; }

  public int ReceiveBufferSize
  {
    get
    {
      return this.receiveBufferSize;
    }
    set
    {
      this.receiveBufferSize = value;
      this.DoCheckBufferSize();
    }
  }

  public int SendBufferSize
  {
    get
    {
      return this.sendBufferSize;
    }
    set
    {
      this.sendBufferSize = value;
      this.DoCheckBufferSize();
    }
  }

  private void DoCheckBufferSize()
  {
    if (this.connector == null) return;
    this.connector.ReceiveBufferSize = this.receiveBufferSize;
    this.connector.SendBufferSize = this.sendBufferSize;
  }

  public event EventHandler Connected;
  public event EventHandler Disconnected;

  private readonly ManualResetEvent shutdown = new(false);

  private class WaitPacket
  {
    private IJsonRpcPacket receivedPacket;

    public int PacketId;
    public readonly AutoResetEvent Signal = new(false);
    public Type WaitType;
    public object Context;
    public bool Dump;

    public IJsonRpcPacket ReceivedPacket
    {
      get
      {
        return this.receivedPacket;
      }
      set
      {
        this.receivedPacket = value;
        this.Signal.Set();
      }
    }
  }

  private readonly List<WaitPacket> waits = new();

  [Obsolete("Use Start() with Connector parameter instead")]
  public bool Start(object handler, int session, Socket socket = null, string address = null)
  {
    var sc = socket == null ? new SocketConnector(address) : new SocketConnector(socket);
    sc.Reconnect = false;
    return this.Start(handler, sc, session);
  }

  /// <summary>
  /// Activates a JSON-RPC peer
  /// </summary>
  /// <param name="handler">Event implementation object. Each event method should be decorated with PacketName attribute</param>
  /// <param name="connector">Connector descendant instance</param>
  /// <param name="session">Session number for server sider peer or cero for client side</param>
  /// <returns></returns>
  public bool Start(object handler, Connector connector, int session = 0)
  {
    if (!this.binder.IsBound) this.binder.BindMethods(handler);
    return this.InternalStart(connector, session);
  }

  /*public bool Start<T>(Connector connector, T handler, int session = 0)
  {
    if (!this.binder.IsBound) this.binder.BindMethods<T>(handler);
    return this.InternalStart(connector, session);
  }*/

  private bool InternalStart(Connector connector, int session)
  {
    this.session = session;
    this.connector = connector;

    try
    {
      const int bufsize = 1024 * 1024 * 6;
      this.ReceiveBufferSize = bufsize;
      this.SendBufferSize = bufsize;

      this.DoCheckBufferSize();
      this.connector.Sizer = this.bufr.GetSize;
      return this.connector.Activate(this);
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
      return false;
    }
  }

  public void Stop()
  {
    try
    {
      lock (this.sendLock)
      {
        this.waits.ForEach(i => i.Signal.Set());
        this.waits.Clear();
      }

      this.shutdown.Set();

      if (this.connector != null && this.connector.IsConnected)
        this.connector.Deactivate();

      this.connector = null;
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  public bool IsConnected { get { return this.connector != null && this.connector.IsConnected; } }

  public void Dispose()
  {
    this.Stop();
    GC.SuppressFinalize(this);
  }

  private static bool CanLogMethod(string name)
  {
    return !Nologkeys.Any(i => i.EqualsOrdinalIgnoreCase(name));
  }

  private WaitPacket FindPacket(int id)
  {
    if (id <= 0)
      return null;

    lock (this.sendLock)
    {
      var ix = this.waits.FindIndex(i => i.PacketId == id);

      if (ix <= -1)
        return null;

      var packet = this.waits[ix];
      this.waits.RemoveAt(ix);

      return packet;
    }
  }

  void IConnectorDataEvents.DataReceived(Connector sender, ReadOnlySpan<byte> buffer)
  {
    try
    {
      var buf = Encoding.UTF8.GetString(buffer);
      var context = sender.Context;
      var wait = this.FindPacket(GetPacketId(buf));
      var methodName = wait == null ? GetPacketMethod(buf) : null;
      var methodFound = !string.IsNullOrEmpty(methodName);
      var unsolicited = methodFound ? this.binder.Methods.FirstOrDefault(i => i.Name == methodName) : null;
      IJsonRpcPacket? packet = null;

      try
      {
        Type? ptype = null;

        if (wait != null)
        {
          if (wait.Context == null || wait.Context.Equals(context))
            ptype = wait.WaitType;
        }
        else if (unsolicited != null)
          ptype = unsolicited.ParamType;

        if (ptype != null)
          packet = JsonSerializer.Deserialize(buf, ptype, JsonTool.Options) as IJsonRpcPacket;
      }
      catch (Exception ex)
      {
        if (this.Dump)
          Trace.Error($"deserialization failed: received=[1:{this.Code}] {buf[..Math.Min(buf.Length, this.DumpSize)]}");

        if (wait != null)
          wait.ReceivedPacket = new JsonRpcPacket { Error = new JsonRpcException(-1, ex.Message) };

        return;
      }

      if (packet != null)
      {
        if (packet.Session > 0)
        {
          if (this.session != 0 && this.session != packet.Session)
          {
            if (this.Dump)
              Trace.Debug($"received=[2:{this.Code}] {buf[..Math.Min(buf.Length, this.DumpSize)]}");

            Trace.Error($"session mismatch received={packet.Session} expected={this.session} method={packet.Method}");
            unsolicited = null;
          }
          else if (this.session == 0)
            this.session = packet.Session;
        }

        if (unsolicited != null)
        {
          if (this.Dump && CanLogMethod(methodName))
            Trace.Debug($"received=[3:{this.Code}] {buf[..Math.Min(buf.Length, this.DumpSize)]}");

          this.binder.Dispatch(new MethodDispatch(unsolicited.Method, packet, context));
        }
        else if (wait != null && wait.PacketId > 0)
        {
          if (this.Dump && wait.Dump)
            Trace.Debug($"received=[4:{this.Code}] {buf[..Math.Min(buf.Length, this.DumpSize)]}");

          wait.ReceivedPacket = packet;
        }
        else
        {
          if (this.Dump && CanLogMethod(methodName))
            Trace.Debug($"received=[5:{this.Code}] {buf[..Math.Min(buf.Length, this.DumpSize)]}");

          Trace.Warning($"method not bound. name={packet.Method}");
        }
      }
      else
      {
        if (this.Dump)
          Trace.Debug($"received=[6:{this.Code}] {buf[..Math.Min(buf.Length, this.DumpSize)]}");

        Trace.Error($"JsonRpcPeer.DataReceived:error=unknown packet name={methodName}");
      }
    }
    catch (Exception ex)
    {
      Trace.Error($"JsonRpcPeer.DataReceived:error={ex}");
    }
  }

  private static string GetPacketMethod(string buf)
  {
    return JsonParser.GetHeaderValue(buf, "method");
  }

  private static int GetPacketId(string buf)
  {
    return int.TryParse(JsonParser.GetHeaderValue(buf, "id"), out int value) ? value : 0;
  }

  public bool SendPacket<TPar, TRes>(TPar data, out TRes response)
    where TPar : class, new()
    where TRes : class, new()
  {
    return this.SendPacket(data, out response, null);
  }

  public bool SendPacket<TPar, TRes>(TPar data, out TRes response, object context)
    where TPar : class, new()
    where TRes : class, new()
  {
    WaitPacket wait;
    IJsonRpcPacket<TPar> packet;

    try
    {
      if (!Monitor.TryEnter(this.sendLock, 0))
      {
        Trace.Debug("SendPacket: waiting operation to complete");
        var lap = DateTime.UtcNow;
        Monitor.Enter(this.sendLock);
        Trace.Debug($"SendPacket: operation complete on lap={(DateTime.UtcNow - lap).TotalMilliseconds}");
      }

      response = null;
      packet = this.CreateRequest(data);

      wait = new WaitPacket
      {
        PacketId = packet.Id,
        WaitType = typeof(JsonRpcResponse<TRes>),
        Dump = CanLogMethod(packet.Method),
        Context = context
      };

      this.waits.Add(wait);
    }
    finally
    {
      Monitor.Exit(this.sendLock);
    }

    var isok = this.UnlockedPostPacket(packet, context);

    if (!isok || !wait.Signal.WaitOne(this.ReceiveTimeout))
    {
      Trace.Warning($"SendPacket: timeout method={packet.Method}");
      this.waits.Remove(wait);
      return false;
    }

    if (wait.ReceivedPacket.Error != null)
      throw new Exception(wait.ReceivedPacket.Error.Message);

    isok = wait.ReceivedPacket != null;
    response = ((IJsonRpcPacket<TRes>)wait.ReceivedPacket).Data;
    return isok;
  }

  public bool PostPacket<T>(T data = null) where T : class, new()
  {
    return this.PostPacket(data, null);
  }

  public bool PostPacket<T>(T data, object context = null) where T : class, new()
  {
    var packet = this.CreateNotification<T>();
    if (data != null) packet.Data = data;
    return this.PostPacket(packet, context);
  }

  public bool Response<T>(T data = null) where T : class, new()
  {
    return this.Response(data, this.binder.PacketInfo.Value.Context);
  }

  public bool Response<T>(T data, object context = null) where T : class, new()
  {
    var packet = this.CreateResponse<T>();
    if (data != null) packet.Data = data;
    return this.PostPacket(packet, context);
  }

  private static bool IsDefault(MemberInfo info, object value)
  {
    if (value == null) return true;

    var attribute = info.GetCustomAttribute<DefaultValueAttribute>();
    if (attribute != null) return attribute.Value.Equals(value);

    var t = value.GetType();

    if (t.IsPrimitive)
    {
      if (t == Typer.TypeInt) return (int)value == 0;
      if (t == Typer.TypeShort) return (short)value == 0;
      if (t == Typer.TypeSByte) return (sbyte)value == 0;
      if (t == Typer.TypeLong) return (long)value == 0;
      if (t == Typer.TypeUInt) return (uint)value == 0;
      if (t == Typer.TypeUShort) return (ushort)value == 0;
      if (t == Typer.TypeByte) return (byte)value == 0;
      if (t == Typer.TypeULong) return (ulong)value == 0;
      if (t == Typer.TypeFloat) return (float)value == 0f;
      if (t == Typer.TypeDouble) return (double)value == 0d;
    }
    else if (t.IsArray) return ((Array)value).Length == 0;

    return false;
  }

  public bool PostPacket<T>(IJsonRpcPacket<T> packet)
  {
    return this.PostPacket(packet, null);
  }

  public bool PostPacket<T>(IJsonRpcPacket<T> packet, object context)
  {
    try
    {
      if (!Monitor.TryEnter(this.sendLock, 0))
      {
        Trace.Debug("PostPacket: waiting operation to complete");
        var lap = DateTime.UtcNow;
        Monitor.Enter(this.sendLock);
        Trace.Debug($"PostPacket: operation complete on lap={(DateTime.UtcNow - lap).TotalMilliseconds}");
      }

      return this.UnlockedPostPacket(packet, context);
    }
    finally
    {
      Monitor.Exit(this.sendLock);
    }
  }

  private bool UnlockedPostPacket<T>(IJsonRpcPacket<T> packet, object context)
  {
    if (this.connector != null && this.connector.IsConnected)
    {
      var fields = typeof(T).GetFields();
      var props = typeof(T).GetProperties();

      if (fields.All(i => IsDefault(i, i.GetValue(packet.Data))) && props.All(i => IsDefault(i, i.GetValue(packet.Data, null))))
      {
        packet.Data = default;
      }

      var jsonstr = JsonSerializer.Serialize(packet, JsonTool.Options);

      if (this.Dump && CanLogMethod(packet.Method))
      {
        Trace.Debug($"sent=[1:{this.Code}] {jsonstr[..Math.Min(jsonstr.Length, this.DumpSize)]}");
      }

      this.connector.Context = context;
      var buffer = Encoding.UTF8.GetBytes(jsonstr);
      return this.connector.Write(buffer) > 0;
    }

    throw new Exception("Peer.PostPacket:not connected");
  }

  void IConnectorEvents.ConnectionEstablished(Connector sender)
  {
    this.closeNotified = false;
    ThreadRunner.Spawn(this.DoConnectionEstablished, sender);
  }

  private void DoConnectionEstablished(object state)
  {
    if (this.connector?.IsConnected ?? false)
      this.Connected?.Invoke(state, null);
  }

  void IConnectorEvents.ConnectionClosed(Connector sender, bool lost)
  {
    if (this.closeNotified)
      return;

    this.session = 0;
    this.closeNotified = true;
    this.Disconnected?.Invoke(this, null);
  }

  public T CreateCommandProxy<T>() where T : class
  {
    return JsonRpcProxy.Create<T>(this);
  }
}
