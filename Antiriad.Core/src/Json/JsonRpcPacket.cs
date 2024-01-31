using System.Text.Json.Serialization;

namespace Antiriad.Core.Json;

public interface IJsonRpcPacket
{
  string Method { get; set; }
  int Session { get; set; }
  int Id { get; set; }
  JsonRpcException Error { get; set; }
}

public interface IJsonRpcPacket<T> : IJsonRpcPacket
{
  T Data { get; set; }
}

public class JsonRpcPacket : IJsonRpcPacket
{
  [JsonPropertyName("jsonrpc")]
  public string JsonRpc { get { return "2.0"; } }

  [JsonPropertyName("method")]
  public string Method { get; set; }

  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("session")]
  public int Session { get; set; }

  [JsonPropertyName("error")]
  public JsonRpcException Error { get; set; }
}

public class JsonRpcPacket<T> : JsonRpcPacket, IJsonRpcPacket<T> where T : class
{
  [JsonPropertyName("params")]
  public virtual T Data { get; set; }
}

public class JsonRpcResponse<T> : JsonRpcPacket<T> where T : class
{
  [JsonPropertyName("result")]
  public override T Data { get; set; }
}
