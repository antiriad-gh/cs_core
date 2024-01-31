using Antiriad.Core.Helpers;

namespace Antiriad.Core.Json;

public class JsonParser
{
  private enum TokenType
  {
    None,
    Identifier,
    Value
  }

  public static string GetHeaderValue(string buffer, string identifier)
  {
    var insideStringLiteral = false;
    var escapedChar = false;
    var currentTokenType = TokenType.Identifier;
    var currentIdentifier = string.Empty;
    var currentValue = string.Empty;
    var foundIdentifier = false;
    var level = 0;

    for (var i = 0; i < buffer.Length; i++)
    {
      var c = buffer[i];

      if (!insideStringLiteral)
      {
        switch (c)
        {
          case '"':
            insideStringLiteral = true;
            break;
          case ':':
            currentTokenType = TokenType.Value;
            break;
          case '{':
            level++;
            break;
          case '}':
            if (--level == 0 && foundIdentifier && currentTokenType == TokenType.Value)
              return currentValue;
            break;
          case ' ':
            break;
          case ',':
            if (level == 1 && foundIdentifier)
              return currentValue;
            currentTokenType = TokenType.Identifier;
            break;
          default:
            if (level == 1 && foundIdentifier && currentTokenType == TokenType.Value)
              currentValue += c;
            break;
        }
      }
      else
      {
        if (escapedChar)
          escapedChar = false;
        else
          switch (c)
          {
            case '"':
              insideStringLiteral = false;

              if (level == 1)
              {
                if (currentTokenType == TokenType.Identifier &&
                    currentIdentifier.EqualsOrdinalIgnoreCase(identifier))
                  foundIdentifier = true;
                else if (foundIdentifier && currentTokenType == TokenType.Value)
                  return currentValue;
              }

              currentIdentifier = "";
              currentValue = "";
              break;
            case '\\':
              escapedChar = true;
              break;
            default:
              {
                if (level == 1)
                {
                  switch (currentTokenType)
                  {
                    case TokenType.Identifier:
                      currentIdentifier += c;
                      break;
                    case TokenType.Value:
                      if (foundIdentifier)
                        currentValue += c;
                      break;
                  }
                }
                break;
              }
          }
      }
    }

    return string.Empty;
  }
}
