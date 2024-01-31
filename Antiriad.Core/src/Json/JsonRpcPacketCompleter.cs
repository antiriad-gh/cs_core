namespace Antiriad.Core.Json;

public class JsonRpcPacketCompleter
{
  private bool insideStringLiteral;
  private bool escapedChar;
  private int braceCounter;
  private int dataSize;

  private void Reset()
  {
    this.insideStringLiteral = false;
    this.escapedChar = false;
    this.braceCounter = 0;
    this.dataSize = 0;
  }

  /// <summary>
  /// Finds a complete JSON packet
  /// </summary>
  /// <param name="buffer">Cumulative byte array</param>
  /// <param name="offset">Index of new appended data</param>
  /// <param name="count">Quantity of bytes to process</param>
  /// <returns>Count of bytes to read containing complete JSON</returns>
  public int GetSize(ReadOnlySpan<byte> buffer)
  {
    if (buffer.Length == 0)
    {
      this.Reset();
      return -2;
    }

    for (; this.dataSize < buffer.Length; this.dataSize++)
    {
      var c = buffer[this.dataSize];

      if (!this.insideStringLiteral)
      {
        switch (c)
        {
          case 123: // {
            this.braceCounter++;
            break;
          case 125: // }
            if (--this.braceCounter == 0)
            {
              var totalSize = this.dataSize + 1;
              this.Reset();
              return totalSize;
            }
            break;
          case 34: // " string open
            this.insideStringLiteral = true;
            break;
        }
      }
      else
      {
        if (this.escapedChar)
        {
          this.escapedChar = false;
        }
        else
        {
          switch (c)
          {
            case 34: // " string close
              this.insideStringLiteral = false;
              break;
            case 92: // \
              this.escapedChar = true;
              break;
          }
        }
      }
    }

    if (this.braceCounter != 0)
      return -3; // next call with last segment only

    this.Reset();
    return -2;
  }
}
