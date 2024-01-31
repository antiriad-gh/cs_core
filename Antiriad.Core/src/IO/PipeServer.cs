using System.IO.Pipes;
using Antiriad.Core.Log;

namespace Antiriad.Core.IO;

/// <summary>
/// PipeServer class.
/// </summary>
public class PipeServer : ConnectorServer
{
  private readonly int maxPipeCount = 10;
  private NamedPipeServerStream server;
  private bool isListening;

  public PipeServer(string name) : this(name, 10) { }

  /// <summary>
  /// Initializes a new instance of the <see cref="PipeServer" /> class.
  /// </summary>
  /// <param name="name">Pipe name.</param>
  /// <param name="maxPipeCount">Limit client count</param>
  public PipeServer(string name, int maxPipeCount) : base(name)
  {
    this.maxPipeCount = maxPipeCount;
  }

  protected override void DeactivateDevice()
  {
    try
    {
      var last = this.server;
      last?.Close();
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  public override bool IsListening
  {
    get { return this.isListening; }
  }

  protected override void ListenThread()
  {
    try
    {
      this.isListening = true;

      while (!this.stop)
      {
        this.server = new NamedPipeServerStream(this.Name, PipeDirection.InOut, this.maxPipeCount,
          PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

        try
        {
          var result = this.server.BeginWaitForConnection(null, null);
          this.server.EndWaitForConnection(result);
        }
        catch (Exception) { }

        if (this.stop || !this.server.IsConnected)
          continue;

        this.FireClientConnected(new PipeConnector(this.server, MillisecondsConstant));
        this.server = null;
      }
    }
    finally
    {
      this.isListening = false;
    }
  }
}
