namespace Antiriad.Core.IO;

/// <summary>
/// Interface for receiving Connector data
/// </summary>
public interface IConnectorDataEvents : IConnectorEvents
{
  void DataReceived(Connector sender, ReadOnlySpan<byte> buffer);
}
