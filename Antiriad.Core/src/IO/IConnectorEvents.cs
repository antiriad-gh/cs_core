namespace Antiriad.Core.IO;

/// <summary>
/// Connection events
/// </summary>
public interface IConnectorEvents
{
  /// <summary>
  /// Received when a connection is established
  /// </summary>
  /// <param name="sender">Instance that made the connection</param>
  void ConnectionEstablished(Connector sender);

  /// <summary>
  /// Received when a connection is closed
  /// </summary>
  /// <param name="sender">Instance that is disconnecting</param>
  /// <param name="lost">True if disconnection is unexpected</param>
  void ConnectionClosed(Connector sender, bool lost);
}
