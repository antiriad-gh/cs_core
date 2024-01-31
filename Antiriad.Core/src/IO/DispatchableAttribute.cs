using Antiriad.Core.Serialization;

namespace Antiriad.Core.IO;

public enum HashSource
{
  None,
  FirstParameter,
  AllParameters
}

/// <summary>
/// Dispatchable attribute for method binding
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DispatchableAttribute : Attribute
{
  /// <summary>
  /// Default constructor with calculated Id
  /// </summary>
  public DispatchableAttribute()
  {
    this.HashSource = HashSource.FirstParameter;
  }

  /// <summary>
  /// Constructor with given Id
  /// </summary>
  /// <param name="id"></param>
  public DispatchableAttribute(int id)
  {
    this.HashSource = HashSource.None;
    this.Id = id;
  }

  public DispatchableAttribute(string id)
  {
    this.HashSource = HashSource.None;
    this.Id = NaibStream.CalculateHash(id);
  }

  public DispatchableAttribute(HashSource source)
  {
    this.HashSource = source;
  }

  public HashSource HashSource { get; private set; }

  /// <summary>
  /// Optional Method's Id
  /// </summary>
  public int Id { get; private set; }
}
