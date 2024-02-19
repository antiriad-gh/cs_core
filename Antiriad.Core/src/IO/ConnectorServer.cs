using Antiriad.Core.Log;

namespace Antiriad.Core.IO;

public class ConnectorServer
{
  private Thread? listener;
  private IConnectorListenerEvents? events;
  protected volatile bool stop;

  /// <summary>
  /// Milliseconds constant
  /// </summary>
  protected const int MillisecondsConstant = 5000;

  /// <summary>
  /// Name number.
  /// </summary>
  public string Name { get; set; }

  public ConnectorServer() : this(string.Empty) { }

  public ConnectorServer(string name)
  {
    this.Name = name;
  }

  public void Activate(IConnectorListenerEvents events)
  {
    if (events == null)
      Trace.Warning("ConnectorServer: null events");

    this.stop = false;
    this.events = events;
    this.Listen();
  }

  protected virtual void Listen()
  {
    this.listener = new Thread(this.ListenThread);
    this.listener.Start();
  }

  public void Deactivate()
  {
    this.stop = true;

    try
    {
      this.DeactivateDevice();

      if (this.listener != null && Environment.CurrentManagedThreadId != this.listener.ManagedThreadId)
      {
        this.listener.Join(MillisecondsConstant);
        this.listener = null;
      }
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  public virtual bool IsListening
  {
    get { return false; }
  }

  protected virtual void ListenThread() { }
  protected virtual void DeactivateDevice() { }

  protected void FireClientConnected(Connector client)
  {
    if (this.listener == null)
    {
      Trace.Debug("no listener. disconnecting");
      try { client.Deactivate(); }
      catch { }
      return;
    }

    try
    {
      this.events?.ClientConnected(client);
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }
}
