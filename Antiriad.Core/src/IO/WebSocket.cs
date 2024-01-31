using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Antiriad.Core.Collections;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.IO;

public class WebSocket : SocketConnector, IConnectorDataEvents
{
  private const string MagicWord = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
  private const string NewLine = "\r\n";
  private static readonly SHA1 Sha1 = SHA1.Create();

  private int bufferIndex;
  private byte[] bufferMask;
  private bool started;
  private IConnectorDataEvents remoteListener;

  private int GetSize(ReadOnlySpan<byte> buffer)
  {
    if (this.started)
    {
      if (buffer.Length < 2)
        return -1;

      var dataLength = buffer[1] & 127;
      var indexMask = dataLength == 126 ? 4 : dataLength == 127 ? 10 : 2;

      this.bufferIndex = indexMask + 4;
      this.bufferMask = buffer[indexMask..(indexMask + 4)].ToArray();

      var realLength = indexMask == 4 ? Bytes.ToUShort(buffer, 2, false) : indexMask == 10 ? (int)Bytes.ToULong(buffer, 2, false) : dataLength;
      return realLength + this.bufferIndex;
    }

    if (buffer != null)
    {
      var len = buffer.Length;

      for (var i = 0; i < len - 3; i++)
      {
        if (buffer[i] == 13 && buffer[i + 1] == 10 && buffer[i + 2] == 13 && buffer[i + 3] == 10)
          return i + 4;
      }
    }

    return -1; // incomplete packet
  }

  public override bool Activate(object listener)
  {
    this.remoteListener = listener as IConnectorDataEvents;
    return base.Activate(this);
  }

  public void ConnectionEstablished(Connector sender)
  {
    this.started = false;
    this.remoteListener?.ConnectionEstablished(sender);
  }

  public void ConnectionClosed(Connector sender, bool lost)
  {
    this.remoteListener?.ConnectionClosed(sender, lost);
  }

  public WebSocket(Socket socket) : base(socket)
  {
    this.Init();
  }

  public WebSocket(string address) : base(address)
  {
    this.Init();
  }

  public WebSocket(string host, int port, int timeout) : base(host, port, timeout)
  {
    this.Init();
  }

  private void Init()
  {
    this.Sizer = this.GetSize;
  }

  public void DataReceived(Connector sender, ReadOnlySpan<byte> buffer)
  {
    if (!this.started)
    {
      var header = Encoding.UTF8.GetString(buffer);
      var lines = header.Split(new[] { NewLine }, StringSplitOptions.RemoveEmptyEntries);
      var keyline = lines.Find(i => i.StartsWith("Sec-WebSocket-Key"));
      var key = keyline != null ? keyline.Split(':')[1].Trim() + WebSocket.MagicWord : string.Empty;
      var key64 = Convert.ToBase64String(Sha1.ComputeHash(Encoding.UTF8.GetBytes(key)));

      var list =
        $"HTTP/1.1 101 Switching Protocols{NewLine}" +
        $"Upgrade: websocket{NewLine}" +
        $"Connection: Upgrade{NewLine}" +
        $"Sec-WebSocket-Accept: {key64}{NewLine}{NewLine}";

      this.Write(Encoding.UTF8.GetBytes(list));
      this.started = true;
    }
    else
    {
      var rbuf = this.DecodeMessage(buffer);

      if (rbuf.Length == 0)
        this.Deactivate();
      else
        this.remoteListener?.DataReceived(this, rbuf);
    }
  }

  protected override int DeviceWrite(byte[] buffer, int offset, int count)
  {
    var enc = !this.started ? buffer : EncodeMessage(buffer, offset, count);
    return base.DeviceWrite(enc, 0, enc.Length);
  }

  private ReadOnlySpan<byte> DecodeMessage(ReadOnlySpan<byte> bytes)
  {
    var size = bytes.Length;
    Span<byte> decoded = new byte[size - this.bufferIndex];

    for (int i = this.bufferIndex, j = 0; i < size; i++, j++)
      decoded[j] = (byte)(bytes[i] ^ this.bufferMask[j % 4]);

    return decoded;
  }

  private static byte[] EncodeMessage(byte[] message, int offset, int size)
  {
    var frame = new byte[] { 129, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    int indexStartRawData;

    if (size <= 125)
    {
      frame[1] = (byte)size;
      indexStartRawData = 2;
    }
    else if (size >= 126 && size <= 0xffff)
    {
      frame[1] = 126;
      Bytes.FromUShort((ushort)size, frame, 2, false);
      indexStartRawData = 4;
    }
    else
    {
      frame[1] = 127;
      Bytes.FromULong((ulong)size, frame, 2, false);
      indexStartRawData = 10;
    }

    var response = new byte[indexStartRawData + size];
    var responseIdx = 0;

    for (var i = 0; i < indexStartRawData; i++)
    {
      response[responseIdx] = frame[i];
      responseIdx++;
    }

    for (var i = 0; i < size; i++)
    {
      response[responseIdx] = message[i + offset];
      responseIdx++;
    }

    return response;
  }
}
