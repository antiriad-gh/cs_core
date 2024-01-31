using System.Net;
using System.Net.Sockets;
using Antiriad.Core.Collections;
using Antiriad.Core.Log;

namespace Antiriad.Core.IO;

public class SocketServer : ConnectorServer
{
  private readonly Dictionary<SocketOptionName, int> options = new();
  private readonly bool asyncIO;
  private Socket? server;
  private bool listening;
  public ManualResetEvent? acceptFinished;

  public string? Host { get; private set; }

  public int Port { get; private set; }

  public override bool IsListening
  {
    get { return this.server != null && this.server.IsBound && this.listening; }
  }

  public int MaximumPending { get; set; } = 10;

  public SocketServer(string address, bool asyncIO = false) : base(address)
  {
    var ep = SocketEndPoint.ParseAddress(address, true);

    this.asyncIO = asyncIO;
    this.Host = ep.Host;
    this.Port = ep.Port;

    if (this.asyncIO)
    {
      this.acceptFinished = new(false);
    }
  }

  public void SetOption(SocketOptionName name, object value)
  {
    this.options[name] = (int)(value is bool boolean ? boolean ? 1 : 0 : value);
  }

  protected override void DeactivateDevice()
  {
    try
    {
      this.stop = true;
      var last = this.server;

      if (last != null)
      {
        this.acceptFinished?.Set();
        last.Close(0);
      }
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  protected override void ListenThread()
  {
    do
    {
      var first = true;

      while (!this.stop)
      {
        if (!this.IsListening)
        {
          try
          {
            this.SetupServer();
            first = true;
          }
          catch (Exception ex)
          {
            if (first)
            {
              first = false;
              Trace.Exception(ex);
            }

            Thread.Sleep(1000);
            continue;
          }
        }

        try
        {
          if (this.asyncIO)
          {
            this.acceptFinished!.Reset();
            this.server?.BeginAccept(this.EndAcceptClient, null);
            this.acceptFinished.WaitOne();
          }
          else if (this.server?.Poll(1000, SelectMode.SelectRead) ?? false)
          {
            var client = this.server.Accept();
            this.ClientConnected(client);
          }
        }
        catch (Exception e)
        {
          if (e is not (NotSupportedException or SocketException or ObjectDisposedException))
          {
            Trace.Exception(e);
          }

          this.stop = true;
        }
      }
    }
    while (!this.stop);

    this.listening = false;
  }

  private void SetupServer()
  {
    var addr = this.Host.Length == 0 || this.Host == "0" || this.Host.ToLower() == "localhost" ? IPAddress.Any : IPAddress.Parse(this.Host);

    this.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    this.options.ForEach(i => this.server.SetSocketOption(SocketOptionLevel.Socket, i.Key, i.Value));
    this.server.Bind(new IPEndPoint(addr, this.Port));
    this.server.Listen(this.MaximumPending);
    this.listening = true;

    Trace.Message($"listening on {addr}:{this.Port}");
  }

  private void EndAcceptClient(IAsyncResult ar)
  {
    try
    {
      var client = this.server?.EndAccept(ar);
      this.acceptFinished!.Set();
      this.ClientConnected(client);
    }
    catch { }
  }

  private void ClientConnected(Socket? client)
  {
    if (client?.Connected ?? false)
    {
      var from = (client.RemoteEndPoint is IPEndPoint ep) ? $"from {ep.Address}:{ep.Port}" : string.Empty;
      Trace.Message($"client connected {from}");
      this.FireClientConnected(new SocketConnector(client, this.asyncIO) { Reconnect = false });
    }
  }
}
