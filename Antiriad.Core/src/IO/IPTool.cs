using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Antiriad.Core.IO;

public static class IPTool
{
  public static List<IPAddress> GetLocalIPAddress()
  {
    var host = Dns.GetHostEntry(Dns.GetHostName());
    var list = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
    return list.ToList();
  }


  public static IPAddress GetSubnetMask(this IPAddress address)
  {
    foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
    {
      foreach (var unicastAddr in adapter.GetIPProperties().UnicastAddresses)
      {
        if (unicastAddr.Address.AddressFamily != AddressFamily.InterNetwork)
          continue;
        if (address.Equals(unicastAddr.Address))
          return unicastAddr.IPv4Mask;
      }
    }

    return IPAddress.None;
  }

  public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
  {
    var ipAdressBytes = address.GetAddressBytes();
    var subnetMaskBytes = subnetMask.GetAddressBytes();

    if (ipAdressBytes.Length != subnetMaskBytes.Length)
      throw new ArgumentException("IP address and subnet mask do not match.");

    var broadcastAddress = new byte[ipAdressBytes.Length];

    for (var i = 0; i < broadcastAddress.Length; i++)
      broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));

    return new IPAddress(broadcastAddress);
  }

  public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
  {
    var ipAdressBytes = address.GetAddressBytes();
    var subnetMaskBytes = subnetMask.GetAddressBytes();

    if (ipAdressBytes.Length != subnetMaskBytes.Length)
      throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

    var broadcastAddress = new byte[ipAdressBytes.Length];

    for (var i = 0; i < broadcastAddress.Length; i++)
      broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));

    return new IPAddress(broadcastAddress);
  }

  public static bool SameSubnetAs(this IPAddress address2, IPAddress address, IPAddress subnetMask)
  {
    var network1 = address.GetNetworkAddress(subnetMask);
    var network2 = address2.GetNetworkAddress(subnetMask);
    return network1.Equals(network2);
  }

  public static bool SameSubnetAs(this IPAddress address1, string address2)
  {
    var msk1 = address1.GetSubnetMask();
    var add2 = IPAddress.Parse(address2);
    return address1.SameSubnetAs(add2, msk1);
  }

  public static string GetSubnetmask(string address)
  {
    var firstOctet = ReturnFirtsOctet(address);
    if (firstOctet <= 127)
      return "255.0.0.0";
    if (firstOctet >= 128 && firstOctet <= 191)
      return "255.255.0.0";
    if (firstOctet >= 192 && firstOctet <= 223)
      return "255.255.255.0";
    return "0.0.0.0";
  }

  private static uint ReturnFirtsOctet(string address)
  {
    var byteIP = IPAddress.Parse(address).GetAddressBytes();
    return byteIP[0];
  }
}
