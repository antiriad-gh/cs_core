namespace Antiriad.Core.IO;

using System;
using System.Collections.Generic;
using Antiriad.Core.Log;

public class ConnectorServerListener<T> where T : ConnectorServer
{
  private readonly object clientLock = new();
  private readonly string listenAddress;
  private readonly List<Connector> clients = new();
  private readonly IConnectorListenerEvents connectorListener;
  private ConnectorServer? server;

  /// <summary>
  /// constructor
  /// <param name="listenAddress">tcp address for listening</param>
  /// <param name="connectorListener">client connection event</param>
  /// </summary>
  public ConnectorServerListener(string listenAddress, IConnectorListenerEvents connectorListener)
  {
    this.listenAddress = listenAddress;
    this.connectorListener = connectorListener;
  }

  /// <summary>
  /// true if it is listening
  /// </summary>
  public bool IsListening
  {
    get { return this.server != null && this.server.IsListening; }
  }

  /// <summary>
  /// Gets the clients.
  /// </summary>
  public List<Connector> Clients
  {
    get { return this.clients; }
  }

  /// <summary>
  /// starts listening
  /// </summary>
  public void Activate()
  {
    try
    {
      if (this.server == null)
      {
        var serverInstance = Activator.CreateInstance(typeof(T), this.listenAddress);
        if (serverInstance != null)
          this.server = (T)serverInstance;
      }
      this.server?.Activate(this.connectorListener);
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  /// <summary>
  /// stops listening
  /// </summary>
  public void Deactivate()
  {
    try
    {
      this.server?.Deactivate();

      var clients = new List<Connector>();

      lock (this.clientLock)
      {
        clients.AddRange(this.clients);
      }

      foreach (var client in clients)
      {
        client.Deactivate();
      }

      lock (this.clientLock)
      {
        this.clients.Clear();
      }
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  /// <summary>
  /// remove a TcpBasePeer client
  /// </summary>
  public void RemoveClient(Connector client)
  {
    lock (this.clientLock)
    {
      this.clients.Remove(client);
    }
  }

  /// <summary>
  /// appends a TcpBasePeer client
  /// </summary>
  public void AppendClient(Connector client, bool activate = true)
  {
    if (activate)
      client.Activate();

    lock (this.clientLock)
    {
      this.clients.Add(client);
    }
  }
}
