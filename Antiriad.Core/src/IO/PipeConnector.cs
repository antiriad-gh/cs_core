using System.IO.Pipes;
using Antiriad.Core.Log;

namespace Antiriad.Core.IO;

public class PipeConnector : Connector
{
  public class EndPoint
  {
    public readonly string Machine;
    public readonly string Name;

    public EndPoint() { }

    public EndPoint(string machine, string name)
    {
      this.Machine = machine;
      this.Name = name;
    }

    public override string ToString()
    {
      return $@"\\{this.Machine}\{this.Name}";
    }
  }

  private PipeStream device;
  private readonly PipeConnector.EndPoint endPoint;

  public PipeConnector(string address, int readTimeout = -1)
  {
    // \\server\pipename
    var pars = address.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

    if (pars.Length > 1)
      this.endPoint = new EndPoint(pars[0], pars[1]);
    else if (pars.Length > 0)
      this.endPoint = new EndPoint(".", pars[0]);

    this.ReadTimeout = readTimeout;
  }

  public PipeConnector(string machine, string name, int readTimeout = -1)
  {
    this.endPoint = new EndPoint(machine, name);
    this.ReadTimeout = readTimeout;
  }

  public PipeConnector(PipeStream stream, int readTimeout)
  {
    this.OwnDevice = false;
    this.device = stream;
    this.endPoint = new EndPoint(".", "unknown");
    this.ReadTimeout = readTimeout;
  }

  public override string ToString()
  {
    return this.endPoint.ToString();
  }

  public override bool IsConnected
  {
    get
    {
      var client = this.device;
      return client != null && client.IsConnected;
    }
  }

  protected override int DeviceRead(byte[] buffer, int offset, int count, int timeout)
  {
    var client = this.device;
    var received = client != null ? client.Read(buffer, offset, count) : -1;
    return received;
  }

  protected override int DeviceWrite(byte[] buffer, int offset, int count)
  {
    var client = this.device;
    if (client == null) return 0;
    client.Write(buffer, offset, count);
    return count;
  }

  public override bool Activate(Object listener)
  {
    if (this.OwnDevice) this.Deactivate();
    return base.Activate(listener);
  }

  private void CreateDevice()
  {
    if (this.device != null) return;
    this.device = new NamedPipeClientStream(this.endPoint.Machine, this.endPoint.Name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
  }

  protected override void DeviceConnect()
  {
    try
    {
      this.CreateDevice();

      if (this.IsConnected)
        return;

      if (this.device is NamedPipeClientStream client)
        client.Connect(this.ReadTimeout);
    }
    catch (TimeoutException)
    {
      Trace.Error($"PipeConnector:DeviceConnect: cannot connect to pipe={this} timeout={this.ReadTimeout}");
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  protected override void DeviceDisconnect()
  {
    var client = this.device;
    if (client == null) return;

    client.Close();
    this.device = null;
  }
}
