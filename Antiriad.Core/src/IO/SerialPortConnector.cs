using System.IO.Ports;
using Antiriad.Core.Helpers;
using Antiriad.Core.Log;

namespace Antiriad.Core.IO;

public class SerialPortConnector : Connector
{
  public class EndPoint
  {
    public EndPoint()
    {
      this.BaudRate = 9600;
      this.Parity = Parity.None;
      this.DataBits = 8;
      this.StopBits = StopBits.One;
    }

    public EndPoint(string name, int baudRate, Parity parity, int dataBits, StopBits stopBits)
    {
      this.Name = name;
      this.BaudRate = baudRate;
      this.Parity = parity;
      this.DataBits = dataBits;
      this.StopBits = stopBits;
    }

    public string Name { get; set; }
    public int BaudRate { get; set; }
    public Parity Parity { get; set; }
    public int DataBits { get; set; }
    public StopBits StopBits { get; set; }

    public override string ToString()
    {
      return $@"\\{this.Name}\{this.BaudRate} {this.Parity} {this.DataBits}";
    }
  }

  private SerialPort device;
  private readonly SerialPortConnector.EndPoint endPoint;

  public SerialPortConnector(string address, int readTimeout = -1)
  {
    // COM1,9600,7,N,1
    // None,Odd,Even,Mark,Space,
    // None,One,Two,OnePointFive
    this.endPoint = new EndPoint();
    var parts = address.Split(',');
    if (parts.Length > 0) this.endPoint.Name = parts[0].ToUpper();
    if (parts.Length > 1) this.endPoint.BaudRate = Typer.To<int>(parts[1]);
    if (parts.Length > 2) this.endPoint.DataBits = Typer.To<int>(parts[2]);
    if (parts.Length > 3)
    {
            if (Enum.TryParse(parts[3], true, out Parity parity))
            {
                this.endPoint.Parity = parity;
            }
            else
            {
                if (parts[3] == "N")
                    this.endPoint.Parity = Parity.None;
                else if (parts[3] == "O")
                    this.endPoint.Parity = Parity.Odd;
                else if (parts[3] == "E")
                    this.endPoint.Parity = Parity.Even;
                else if (parts[3] == "M")
                    this.endPoint.Parity = Parity.Mark;
                else if (parts[3] == "S")
                    this.endPoint.Parity = Parity.Space;
            }
    }
    if (parts.Length > 4)
    {
      if (Enum.TryParse(parts[4], true, out StopBits stopBits))
        this.endPoint.StopBits = stopBits;
      else
      {
        if (parts[4] == "0") this.endPoint.StopBits = StopBits.None;
        else if (parts[4] == "1") this.endPoint.StopBits = StopBits.One;
        else if (parts[4] == "2") this.endPoint.StopBits = StopBits.Two;
        else if (parts[4] == "1.5") this.endPoint.StopBits = StopBits.OnePointFive;
      }
    }

    if (readTimeout > 0) this.ReadTimeout = readTimeout;
  }

  public SerialPortConnector(SerialPort device, int readTimeout)
  {
    this.OwnDevice = false;
    this.device = device;
    this.endPoint = new EndPoint(device.PortName, device.BaudRate, device.Parity, device.DataBits, device.StopBits);
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
      return client != null && client.IsOpen;
    }
  }

  public override bool Activate(Object listener)
  {
    if (this.OwnDevice) this.Deactivate();
    return base.Activate(listener);
  }

  protected override void DeviceConnect()
  {
    try
    {
      this.CreateDevice();
      if (this.IsConnected) return;

      var client = this.device;
      client?.Open();
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  private void CreateDevice()
  {
    if (this.device != null) return;
    this.device = new SerialPort(this.endPoint.Name, this.endPoint.BaudRate, this.endPoint.Parity, this.endPoint.DataBits, this.endPoint.StopBits);
  }

  protected override void DeviceDisconnect()
  {
    var client = this.device;
    if (client == null) return;

    client.Close();
    this.device = null;
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
}
