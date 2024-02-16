namespace Antiriad.Core.IO;

/// <summary>
/// Interface for intercept Binder method calling
/// </summary>
public interface IMethodBinderInterceptor
{
  /// <summary>
  /// Called before invoke method
  /// </summary>
  /// <param name="id">Numeric method identification</param>
  /// <param name="data">Payload</param>
  /// <returns>Optionally modified Payload</returns>
  object BeforeInvoke(int id, object? data);

  /// <summary>
  /// Called after invoke method
  /// </summary>
  /// <param name="id"></param>
  /// <param name="data"></param>
  /// <param name="answer"></param>
  void AfterInvoke(int id, object? data, object? answer);
}
