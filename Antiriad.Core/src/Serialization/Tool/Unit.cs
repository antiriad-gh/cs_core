namespace Antiriad.Core.Serialization.Tool;

public enum Unit
{
  Unknown = 0,
  Byte = 1,
  Bool = 2,
  Char = 3,
  Short = 4,
  UShort = 5,
  Int = 6,
  UInt = 7,
  Long = 8,
  ULong = 9,
  Single = 10,
  Double = 11,
  Object = 12,
  DateTime = 13,	// ushort byte byte + byte byte ushort
  String = 14,
  BiasedDateTime = 15, //	datetime sbyte
  Guid = 16, //	uint - ushort - ushort - ushort - ushort ushort ushort
}
