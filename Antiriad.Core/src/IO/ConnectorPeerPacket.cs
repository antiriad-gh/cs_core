using Antiriad.Core.Serialization;
using Antiriad.Core.Log;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.IO;

internal class ConnectorPeerPacket
{
  public const int HeaderSize = 10;
  public const int HeaderSizePos = 2;

  public short ProtocolId;
  public short ProtocolVersion;
  public int PacketId;
  public int Sequence;
  public string? ErrorMessage;
  public bool IsResponse;
  public object? Context;
  public readonly object Data;

  private readonly bool isError;
  private readonly NaibDeserializer dec;
  private int index;

  public bool IsException()
  {
    return !string.IsNullOrEmpty(this.ErrorMessage);
  }

  public ConnectorPeerPacket(NaibDeserializer dec, ReadOnlySpan<byte> buffer, object? context)
  {
    this.Context = context;
    this.ProtocolId = this.ReadShort(buffer);
    this.ReadInt(buffer); // size
    this.ProtocolVersion = this.ReadShort(buffer);
    this.PacketId = this.ReadInt(buffer);
    this.Sequence = this.ReadInt(buffer);
    var flags = this.ReadShort(buffer);
    this.isError = (flags & 0x1) > 0;
    this.IsResponse = (flags & 0x2) > 0;
    this.dec = dec;

    try
    {
      var bytes = new MemoryStream(buffer.ToArray());
      this.Data = this.dec?.Read(bytes);
    }
    catch (Exception)
    {
    }
  }

  public ConnectorPeerPacket(int packetId, int sequence, object data, string? errorMessage, bool response)
  {
    this.IsResponse = response;
    this.PacketId = packetId;
    this.Sequence = sequence;
    this.Data = data;
    this.ErrorMessage = errorMessage;
  }

  public T? Get<T>() where T : class, new()
  {
    try
    {
      if (this.isError) return null;
      return (T)this.Data;
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
      return null;
    }
  }

  public byte[] GetBytes(NaibSerializer enc, short protocolId, short protocolVersion)
  {
    try
    {
      var mem = new MemoryStream();
      short flags = 0;

      if (this.ErrorMessage != null) flags |= 0x1;
      if (this.IsResponse) flags |= 0x2;

      WriteShort(mem, protocolId);
      WriteInt(mem, 0); // size
      WriteShort(mem, protocolVersion);
      WriteInt(mem, this.PacketId);
      WriteInt(mem, this.Sequence);
      WriteShort(mem, flags);

      enc.Write(mem, this.Data);

      mem.Position = ConnectorPeerPacket.HeaderSizePos;
      WriteInt(mem, (int)mem.Length);

      return mem.ToArray();
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
      return Array.Empty<byte>();
    }
  }

  private static void WriteBytes(Stream mem, ReadOnlySpan<byte> buf)
  {
    mem.Write(buf);
  }

  private static void WriteInt(Stream mem, int value)
  {
    WriteBytes(mem, Bytes.FromInt(value));
  }

  private static void WriteShort(Stream mem, short value)
  {
    WriteBytes(mem, Bytes.FromShort(value));
  }

  private int ReadInt(ReadOnlySpan<byte> buffer)
  {
    var result = Bytes.ToInt(buffer[this.index..(this.index + 4)]);
    this.index += 4;
    return result;
  }

  private short ReadShort(ReadOnlySpan<byte> buffer)
  {
    var result = Bytes.ToShort(buffer[this.index..(this.index + 2)]);
    this.index += 2;
    return result;
  }
}
