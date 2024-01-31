using System.Text.Json.Serialization;

namespace Antiriad.Core.Json;

/// <summary>
///  The error codes from and including -32768 to -32000 are reserved for pre-defined errors. 
///
///  code        message             meaning
///
///  -32700      Parse error         Invalid JSON was received by the server.  An error occurred on the server while parsing the JSON text.
///  -32600      Invalid Request     The JSON sent is not a valid Request object.
///  -32601      Method not found    The method does not exist / is not available.
///  -32602      Invalid params      Invalid method parameter(s).
///  -32603      Internal error      Internal JSON-RPC error.
///  -32000 to -32099 Server error   Reserved for implementation-defined server-errors.
/// </summary>
[Serializable]
public class JsonRpcException
{
  [JsonPropertyName("code")]
  public int Code { get; set; }

  [JsonPropertyName("message")]
  public string Message { get; set; }

  [JsonPropertyName("data")]
  public object Data { get; set; }

  public JsonRpcException(int code, string message, object data = null)
  {
    this.Code = code;
    this.Message = message;
    this.Data = data;
  }
}
