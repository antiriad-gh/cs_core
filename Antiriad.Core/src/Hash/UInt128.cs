namespace Antiriad.Core.Hash;

using System;

public class UInt128
{
  public UInt128() { }

  public UInt128(UInt64 low, UInt64 high)
  {
    this.Low = low;
    this.High = high;
  }

  public UInt64 Low { get; set; }
  public UInt64 High { get; set; }

  protected bool Equals(UInt128 other)
  {
    return this.Low == other.Low && this.High == other.High;
  }

  public override bool Equals(object? obj)
  {
    if (obj is null) return false;
    if (ReferenceEquals(this, obj)) return true;
    if (obj.GetType() != this.GetType()) return false;
    return this.Equals((UInt128)obj);
  }

  public override int GetHashCode()
  {
    unchecked
    {
      return (this.Low.GetHashCode() * 397) ^ this.High.GetHashCode();
    }
  }
}
