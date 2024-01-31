using System.Net;
using Antiriad.Core.Collections;
using Antiriad.Core.Helpers;
using Antiriad.Core.Log;

namespace Antiriad.Core.IO;

public class SocketEndPoint
{
  public readonly string? Host;
  public readonly int Port;
  private IPEndPoint? ip;

  public SocketEndPoint() { }

  public SocketEndPoint(IPEndPoint? ip)
  {
    if (ip == null) return;
    this.ip = ip;
    this.Host = ip.Address.ToString();
    this.Port = ip.Port;
  }

  public SocketEndPoint(string host, int port)
  {
    this.Host = host;
    this.Port = port;
  }

  public IPEndPoint? IP
  {
    get
    {
      if (this.ip == null || this.ip.AddressFamily == 0)
        this.ip = Resolve(this);

      return this.ip;
    }
    set
    {
      this.ip = value;
    }
  }

  public bool IsEmpty
  {
    get { return string.IsNullOrEmpty(this.Host) || this.Host == "0" || this.Host == "0.0.0.0"; }
  }

  public bool IsLocalHost
  {
    get { return this.Host?.EqualsOrdinalIgnoreCase("localhost") ?? false; }
  }

  public override bool Equals(object? obj)
  {
    return (obj is SocketEndPoint ep) && ep.IP != null && ep.IP.Equals(this.IP);
  }

  public override string ToString()
  {
    return $"{this.Host}:{this.Port}";
  }

  public override int GetHashCode()
  {
    return base.GetHashCode();
  }

  public static IPEndPoint? Resolve(string address)
  {
    return Resolve(ParseAddress(address));
  }

  public static IPEndPoint? Resolve(SocketEndPoint address)
  {
    if (string.IsNullOrEmpty(address.Host))
      return new IPEndPoint(0, 0);

    if (!IPAddress.TryParse(address.Host, out var ep))
    {
      var ip = Dns.GetHostEntry(address.Host);

      if (ip.AddressList.Length > 0)
        ep = ip.AddressList[0];
      else
        return null;
    }

    return new IPEndPoint(ep, address.Port);
  }

  public static SocketEndPoint ParseAddress(string address)
  {
    return ParseAddress(address, true);
  }

  public static SocketEndPoint ParseAddress(string address, bool listen)
  {
    try
    {
      var param = address.Split(':');

      if (listen)
        new[] { "*", "any", "localhost " }.ForEach(i => param[0] = param[0].Replace(i, "0"));

      if (param[0].Length == 0 || (!listen && (param[0] == "0" || param[0] == "*")) || "localhost".EqualsOrdinalIgnoreCase(param[0]))
        param[0] = "127.0.0.1";

      if (param.Length == 1)
        return new SocketEndPoint(param[0], 0);

      if (param[1].Length == 0)
        param[1] = "0";

      return new SocketEndPoint(param[0], int.Parse(param[1]));
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
      return new SocketEndPoint("127.0.0.1", 0);
    }
  }
}
