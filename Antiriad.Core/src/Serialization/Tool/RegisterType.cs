namespace Antiriad.Core.Serialization.Tool;

[AttributeUsage(AttributeTargets.Class)]
public class RegisterType : Attribute
{
  /// <summary>
  /// Optional Id to avoid calculated Hash from Type Name
  /// </summary>
  public int Id { get; private set; }

  /// <summary>
  /// Optional Name to user when calculate Hash instead of Type name
  /// </summary>
  public string Name { get; private set; }

  /// <summary>
  /// Register a type with a given name to not use Type name
  /// </summary>
  /// <param name="alias"></param>
  public RegisterType(string alias)
  {
    this.Name = alias;
  }

  /// <summary>
  /// Register a type with a given Id to avoid using a Hash from Type name
  /// </summary>
  /// <param name="id"></param>
  public RegisterType(int id)
  {
    this.Id = id;
  }
}
