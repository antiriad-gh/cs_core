namespace Antiriad.Core.IO;

/// <summary>
/// Interface for being notified when a client is connected
/// </summary>
public interface IConnectorListenerEvents
{
  /// <summary>
  /// Received when a client is connected
  /// </summary>
  /// <param name="client"></param>
  void ClientConnected(Connector client);
}
