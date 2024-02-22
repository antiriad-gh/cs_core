using Antiriad.Core.Serialization;
using Antiriad.Core.Threading;
using Antiriad.Core.Log;
using Antiriad.Core.Serialization.Tool;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.IO;

/// <summary>
/// Client Peer for using with Connector descendants. Internally serializes data using NaibSerializer
/// </summary>
public class ConnectorPeer : IConnectorDataEvents, IMethodBinderInterceptor
{
  private readonly short protocolId = 0;
  private readonly short protocolVersion = 0;

  private Connector client;
  private MethodBinder binder;
  private readonly object clientLock = new();

  private class WaitPacket
  {
    public int Sequence;
    public readonly AutoResetEvent Signal = new(false);
    public ConnectorPeerPacket ReceivedPacket;
  }

  public override string ToString()
  {
    return $"{this.client} - Connected={this.IsConnected}";
  }

  private readonly List<WaitPacket> waits = new();
  private readonly NaibTypeInfoList cache = new();
  private NaibSerializer? enc;
  private NaibDeserializer? dec;
  private int sequence;

  /// <summary>
  /// Raised when a connection is established
  /// </summary>
  public event EventHandler ConnectionEstablished;

  /// <summary>
  /// Raised when a connection is closed
  /// </summary>
  public event EventHandler ConnectionClosed;

  public ConnectorPeer() { }

  public ConnectorPeer(Connector client)
  {
    this.Setup(client);
  }

  public ConnectorPeer(Connector client, bool backgroundProcessing, object foreignBind)
  {
    this.Setup(client, backgroundProcessing, foreignBind);
  }

  public void Setup(Connector client, bool backgroundProcessing = false, object? foreignBind = null)
  {
    this.client = client;
    this.client.Sizer = ConnectorPeer.GetSize;
    this.binder = new MethodBinder(backgroundProcessing);
    this.binder.Bind(foreignBind ?? this, this);
  }

  public void TransparentSetup(Connector client, bool backgroundProcessing = false, object? foreignBind = null)
  {
    this.client = client;
    this.client.Sizer = ConnectorPeer.GetSize;
    this.binder = new MethodBinder(backgroundProcessing);
    this.binder.TransparentBind(foreignBind ?? this, this);
  }

  public bool IsConnected { get { return this.client != null && this.client.IsConnected; } }
  public Connector Client { get { return this.client; } }

  public static int GetSize(ReadOnlySpan<byte> buffer)
  {
    return buffer != null && buffer.Length >= ConnectorPeerPacket.HeaderSize ? Bytes.ToInt(buffer[ConnectorPeerPacket.HeaderSizePos..]) : -1;
  }

  public bool Activate(bool reconnect)
  {
    this.client.Reconnect = reconnect;
    return this.client.Activate(this);
  }

  public void Deactivate()
  {
    this.Deactivate(true);
  }

  protected void Deactivate(bool stop)
  {
    lock (this.clientLock)
    {
      this.waits.ForEach(i => i.Signal.Set());
      this.waits.Clear();
    }

    this.client?.Deactivate();
    this.InternalDeactivate();

    if (stop)
      try
      {
        this.client.Sizer = null;
        this.binder.Unbind();
      }
      catch (Exception) { }
  }

  protected virtual void InternalDeactivate() { }

  public bool Post<T>() where T : class, new()
  {
    return this.Post(new T());
  }

  public bool Post(object data)
  {
    return this.Post(NaibTypeInfo.GetHash(data), data, null);
  }

  public bool Post(object data, string errorMessage)
  {
    return this.Post(NaibTypeInfo.GetHash(data), data, errorMessage);
  }

  public bool Post(int packetId, object data, string errorMessage)
  {
    return this.Post(packetId, data, errorMessage, false);
  }

  public bool PostPacket(int packetId, object data)
  {
    return this.Post(new ConnectorPeerPacket(packetId, 0, data, null, false));
  }

  public bool Post(int packetId, object data, string errorMessage, bool response)
  {
    return this.Post(new ConnectorPeerPacket(packetId, 0, data, errorMessage, response));
  }

  private bool Post(ConnectorPeerPacket packet)
  {
    lock (this.clientLock) return this.UnlockedPost(packet);
  }

  private bool UnlockedPost(ConnectorPeerPacket packet)
  {
    if (this.client == null || !this.client.IsConnected)
      return false;

    try
    {
      var buffer = packet.GetBytes(this.enc, this.protocolId, this.protocolVersion);
      this.client.Write(buffer);
      return true;
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
      return false;
    }
  }

  public bool Send<T>(object data, out T reply, int timeout = 0) where T : class, new()
  {
    reply = this.Send<T>(data, timeout);
    return reply != null;
  }

  public T Send<T>(object data, int timeout = 0) where T : class, new()
  {
    var seq = this.GetNextSeq();
    return this.InternalSend<T>(new ConnectorPeerPacket(NaibTypeInfo.GetHash(data), seq, data, null, false), timeout);
  }

  public object SendPacket(int packetId, object data)
  {
    return this.Send(packetId, data, null, -1);
  }

  public object Send(int packetId, object data, string errorMessage, int timeout)
  {
    return this.InternalSend<object>(new ConnectorPeerPacket(packetId, this.GetNextSeq(), data, errorMessage, false), timeout);
  }

  public object Send(int packetId, object data, string errorMessage)
  {
    return this.InternalSend<object>(new ConnectorPeerPacket(packetId, this.GetNextSeq(), data, errorMessage, false), 0);
  }

  private int GetNextSeq()
  {
    lock (this) return ++this.sequence;
  }

  private T InternalSend<T>(ConnectorPeerPacket packet, int timeout) where T : class, new()
  {
    var wait = new WaitPacket { Sequence = packet.Sequence };

    lock (this.clientLock)
    {
      this.waits.Add(wait);

      if (!this.UnlockedPost(packet))
      {
        this.waits.Remove(wait);
        return null;
      }
    }

    var payload = default(T);

    if (wait.Signal.WaitOne(timeout <= 0 ? this.client.ReadTimeout : timeout))
    {
      if (wait.ReceivedPacket.IsException())
      {
        Trace.Error($"Proxy.SendPacket:error remote exception={wait.ReceivedPacket.ErrorMessage}");
        throw new Exception(wait.ReceivedPacket.ErrorMessage);
      }

      payload = wait.ReceivedPacket.Get<T>();
    }
    else
    {
      lock (this.clientLock)
        this.waits.Remove(wait);

      var name = string.Empty;

      try
      {
        var data = packet.Data;
        name = data != null ? data.GetType().FullName : "?";
      }
      catch (Exception ex)
      {
        Trace.Exception(ex);
      }

      Trace.Error($"Proxy.SendPacket:error timeout id={packet.PacketId} name={name} method={this.binder?.GetPacketName(packet.PacketId)}");
    }

    return payload;
  }

  private WaitPacket FindPacket(int sequence)
  {
    lock (this.clientLock)
    {
      var ix = this.waits.FindIndex(i => i.Sequence == sequence);
      if (ix <= -1) return null;
      var packet = this.waits[ix];
      this.waits.RemoveAt(ix);
      return packet;
    }
  }

  void IConnectorDataEvents.DataReceived(Connector sender, ReadOnlySpan<byte> buffer)
  {
    try
    {
      var packet = new ConnectorPeerPacket(this.dec, buffer, sender.Context);
      var wait = this.FindPacket(packet.Sequence);

      if (wait != null)
      {
        wait.ReceivedPacket = packet;
        wait.Signal.Set();
      }
      else
        this.binder?.Dispatch(packet.PacketId, packet);
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  void IConnectorEvents.ConnectionEstablished(Connector sender)
  {
    this.cache.Clear();
    this.enc = new NaibSerializer(this.cache, false);
    this.dec = new NaibDeserializer(this.cache);
    ThreadRunner.Spawn(state => this.FireConnectionEstablished(sender));
  }

  void IConnectorEvents.ConnectionClosed(Connector sender, bool lost)
  {
    this.enc = null;
    this.dec = null;
    ThreadRunner.Spawn(state => this.FireConnectionClosed(sender, lost));
  }

  protected virtual void FireConnectionEstablished(Connector connector)
  {
    this.ConnectionEstablished?.Invoke(this, new EventArgs());
  }

  protected virtual void FireConnectionClosed(Connector connector, bool lost)
  {
    this.ConnectionClosed?.Invoke(this, new EventArgs());
  }

  object IMethodBinderInterceptor.BeforeInvoke(int id, object data)
  {
    return data is ConnectorPeerPacket packet ? packet.Data : data;
  }

  void IMethodBinderInterceptor.AfterInvoke(int id, object data, object answer)
  {
    if (data is ConnectorPeerPacket packet && answer != null)
      this.Post(new ConnectorPeerPacket(packet.PacketId, packet.Sequence, answer, null, true));
  }

  public T? CreateCommandProxy<T>() where T : class
  {
    var proxy = ProxyBuilder.BuildProxyType<T>(typeof(ConnectorPeer));
    return Activator.CreateInstance(proxy, this) as T;
  }
}
