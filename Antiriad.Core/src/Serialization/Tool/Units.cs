using Antiriad.Core.Helpers;

namespace Antiriad.Core.Serialization.Tool;

public static class Units
{
  public static Type[] UnitTypes =
  {
    null!,
    Typer.TypeByte,
    Typer.TypeBoolean,
    Typer.TypeChar,
    Typer.TypeShort,
    Typer.TypeUShort,
    Typer.TypeInt,
    Typer.TypeUInt,
    Typer.TypeLong,
    Typer.TypeULong,
    Typer.TypeFloat,
    Typer.TypeDouble,
    Typer.TypeObject,
    Typer.TypeDateTime,
    Typer.TypeString,
    Typer.TypeDateTimeOffset,
    Typer.TypeGuid,
  };

  public static object[] DefaultValues =
  {
    null!,
    (byte)0,
    false,
    (char)0,
    (short)0,
    (ushort)0,
    0,
    0u,
    0L,
    0ul,
    0f,
    0d,
    null!,
    DateTime.MinValue,
    null!,
    DateTimeOffset.MinValue,
    Guid.Empty,
  };

  public static Unit FromType(Type? type)
  {
    if (type == null) return Unit.Unknown;
    if (type.IsArray) type = type.GetElementType()!;
    if (type == Typer.TypeByte || type == Typer.TypeSByte) return Unit.Byte;
    if (type == Typer.TypeBoolean) return Unit.Bool;
    if (type == Typer.TypeChar) return Unit.Char;
    if (type == Typer.TypeShort) return Unit.Short;
    if (type == Typer.TypeInt) return Unit.Int;
    if (type == Typer.TypeLong) return Unit.Long;
    if (type == Typer.TypeUShort) return Unit.UShort;
    if (type == Typer.TypeUInt) return Unit.UInt;
    if (type == Typer.TypeULong) return Unit.ULong;
    if (type == Typer.TypeFloat) return Unit.Single;
    if (type == Typer.TypeDouble) return Unit.Double;
    if (type == Typer.TypeString) return Unit.String;
    if (type == Typer.TypeDateTime) return Unit.DateTime;
    if (type == Typer.TypeDateTimeOffset) return Unit.BiasedDateTime;
    if (type == Typer.TypeGuid) return Unit.Guid;
    return type.IsClass || type.IsAnsiClass ? Unit.Object : Unit.Unknown;
  }

  public static Type? FromUnit(Unit unit)
  {
    var ix = (int)unit;
    return ix >= 0 && ix < UnitTypes.Length ? UnitTypes[ix] : null;
  }

  public static object? GetDefaultValue(Unit unit)
  {
    var ix = (int)unit;
    return ix >= 0 && ix < DefaultValues.Length ? DefaultValues[ix] : null;
  }
}
