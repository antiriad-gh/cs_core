using System.Text.Json;
using System.Text.Json.Serialization;

namespace Antiriad.Core.Json;

public static class JsonTool
{
  private enum TokenType
  {
    None,
    Identifier,
    Value
  }

  public static readonly JsonSerializerOptions Options = new()
  {
    AllowTrailingCommas = true,
    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    Converters = { new ObjectAsPrimitiveConverter(FloatFormat.Double, UnknownNumberFormat.Error) },
    IncludeFields = true,
  };

  public static object Deserialize(string jsonstr, Type type)
  {
    return JsonSerializer.Deserialize(jsonstr, type, Options)!;
  }

  public static string Serialize(object value)
  {
    return JsonSerializer.Serialize(value, Options);
  }
}

public class ObjectAsPrimitiveConverter : JsonConverter<object>
{
  FloatFormat FloatFormat { get; init; }
  UnknownNumberFormat UnknownNumberFormat { get; init; }

  public ObjectAsPrimitiveConverter(FloatFormat floatFormat, UnknownNumberFormat unknownNumberFormat)
  {
    this.FloatFormat = floatFormat;
    this.UnknownNumberFormat = unknownNumberFormat;
  }

  public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
  {
    if (value.GetType() == typeof(object))
    {
      writer.WriteStartObject();
      writer.WriteEndObject();
    }
    else
    {
      JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
  }

  public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    switch (reader.TokenType)
    {
      case JsonTokenType.Null:
        return null!;

      case JsonTokenType.False:
        return false;

      case JsonTokenType.True:
        return true;

      case JsonTokenType.String:
        return reader.GetString()!;

      case JsonTokenType.Number:
        {
          if (reader.TryGetInt32(out var i))
            return i;

          if (reader.TryGetInt64(out var l))
            return l;

          if (this.FloatFormat == FloatFormat.Decimal && reader.TryGetDecimal(out var m))
            return m;

          if (this.FloatFormat == FloatFormat.Double && reader.TryGetDouble(out var d))
            return d;

          using var doc = JsonDocument.ParseValue(ref reader);

          if (this.UnknownNumberFormat == UnknownNumberFormat.JsonElement)
            return doc.RootElement.Clone();

          throw new JsonException(string.Format("Cannot parse number {0}", doc.RootElement.ToString()));
        }

      case JsonTokenType.StartArray:
        {
          var list = new List<object>();

          while (reader.Read())
          {
            switch (reader.TokenType)
            {
              case JsonTokenType.EndArray:
                return list.ToArray();

              default:
                list.Add(this.Read(ref reader, typeof(object), options));
                break;
            }
          }

          throw new JsonException();
        }

      case JsonTokenType.StartObject:
        {
          var dict = new Dictionary<string, object>();

          while (reader.Read())
          {
            switch (reader.TokenType)
            {
              case JsonTokenType.EndObject:
                return dict;

              case JsonTokenType.PropertyName:
                var key = reader.GetString();
                reader.Read();
                dict.Add(key!, this.Read(ref reader, typeof(object), options));
                break;

              default:
                throw new JsonException();
            }
          }

          throw new JsonException();
        }

      default:
        {
          throw new JsonException(string.Format("Unknown token {0}", reader.TokenType));
        }
    }
  }
}

public enum FloatFormat
{
  Double,
  Decimal,
}

public enum UnknownNumberFormat
{
  Error,
  JsonElement,
}
