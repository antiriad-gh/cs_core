using System.Reflection;
using Antiriad.Core.Serialization;
using Antiriad.Core.Threading;
using Antiriad.Core.Log;
using Antiriad.Core.Serialization.Tool;
using Antiriad.Core.Helpers;
using Antiriad.Core.Types;

namespace Antiriad.Core.IO;

public class MethodBinder
{
  internal delegate void PostDelegate(object obj, object arg);
  internal delegate void SendDelegate(object obj, object arg, object answer);

  internal delegate void ProcDelegate(object obj, object[] args);
  internal delegate object FuncDelegate(object obj, object[] args);

  internal class MethodCall
  {
    public int Id;
    public object Invoker;
    public MethodInfo? Info;
    public Type? AnswerType;

    public MethodCall(object invoker, int methodId, Type? atype, MethodInfo? info = null)
    {
      this.Invoker = invoker;
      this.Id = methodId;
      this.AnswerType = atype;
      this.Info = info;
    }
  }

  internal class QueueItem
  {
    public MethodCall Method;
    public object Data;

    public QueueItem(MethodCall method, object data)
    {
      this.Method = method;
      this.Data = data;
    }
  }

  private readonly List<MethodCall> methods = new();
  private readonly bool async;
  private readonly Queue<QueueItem>? queue;
  private object? handler;
  private IMethodBinderInterceptor? binderInterceptor;
  private volatile bool spawnRunning;

  public MethodBinder(bool async)
  {
    this.async = async;

    if (!async)
      this.queue = new Queue<QueueItem>();
  }

  public bool IsBound() { return this.methods.Count > 0; }

  public void Unbind()
  {
    this.handler = null;
    this.binderInterceptor = null;
    this.methods.Clear();
  }

  public static string GetMethodName(MethodInfo info)
  {
    var paramList = info.GetParameters();
    var paramNames = paramList.Length > 0 ? "?" + string.Join("+", paramList.Select(i => i.ParameterType.Name)) : string.Empty;
    return $"{info.Name}:{info.ReturnType.Name}{paramNames}";
  }

  public static int GetMethodId(MethodInfo info)
  {
    return GetMethodId(GetMethodName(info));
  }

  public static int GetMethodId(string name)
  {
    return NaibStream.CalculateHash(name);
  }

  public void TransparentBind<T>(T handler, IMethodBinderInterceptor? interceptor = null)
  {
    if (handler == null)
      throw new Exception("handler cannot be null");

    var handlerType = handler.GetType();
    this.handler = handler;
    this.methods.Clear();
    this.binderInterceptor = interceptor ?? this.handler as IMethodBinderInterceptor;

    foreach (var info in handlerType.GetMethods())
    {
      if (info.GetCustomAttributes(typeof(MethodBinderIgnoreAttribute), false).Length > 0)
        continue;

      var methodId = GetMethodId(info);
      Trace.Debug($"bound method={info.Name} id={methodId}");

      try
      {
        var returnsVoid = info.ReturnType == typeof(void);
        var mi = MethodGenerator.MakeUntypedDelegateArrayArgument(returnsVoid ? typeof(ProcDelegate) : typeof(FuncDelegate), info, handlerType);
        this.methods.Add(new MethodCall(mi, methodId, returnsVoid ? null : info.ReturnType, info));
      }
      catch (Exception ex)
      {
        Trace.Exception(ex);
      }
    }

    Trace.Debug($"count={this.methods.Count}");
  }

  public void Bind(object handler, object interceptor)
  {
    var handlerType = handler.GetType();
    this.handler = handler;
    this.methods.Clear();
    this.binderInterceptor = (interceptor ?? this.handler) as IMethodBinderInterceptor;

    try
    {
      foreach (var info in handlerType.GetMethods())
      {
        if (info.GetCustomAttributes(typeof(DispatchableAttribute), false) is not DispatchableAttribute[] attr || attr.Length <= 0)
          continue;

        if (info.ReturnType != Typer.TypeVoid)
        {
          Trace.Error($"Proxy.BindMethods() return is not void:method={info.Name}");
          continue;
        }

        var pars = info.GetParameters();

        if (pars.Length != 1 && pars.Length != 2)
        {
          Trace.Error($"Proxy.BindMethods() parameter count error (should be 1 or 2):method={info.Name}");
          continue;
        }

        string methodName;
        int methodId;

        if (pars[0].ParameterType.GetCustomAttributes(typeof(RegisterType), false) is RegisterType[] regtype && regtype.Length > 0)
        {
          methodName = string.IsNullOrEmpty(regtype[0].Name) ? pars[0].ParameterType.Name : regtype[0].Name;
          methodId = NaibTypeInfo.Register(pars[0].ParameterType, methodName, regtype[0].Id);
        }
        else
        {
          methodName = pars[0].ParameterType.FullName;
          methodId = attr[0].HashSource switch
          {
            HashSource.FirstParameter => NaibStream.CalculateHash(methodName),
            HashSource.AllParameters => NaibStream.CalculateHash(string.Join("+", pars.Select(i => i.ParameterType.FullName))),
            _ => attr[0].Id,
          };
        }

        Trace.Debug($"bound method={info.Name} packet={methodName} id={methodId}");

        var delegateType = pars.Length == 1 ? typeof(PostDelegate) : typeof(SendDelegate);
        var atype = pars.Length == 1 ? null : pars[1].ParameterType;
        var mi = MethodGenerator.MakeUntypedDelegate(delegateType, info, handlerType);
        this.methods.Add(new MethodCall(mi, methodId, atype));
      }

      Trace.Debug($"Proxy.BindMethods() count={this.methods.Count}");
    }
    catch (Exception ex)
    {
      Trace.Error($"Proxy.BindMethods():{ex}");
    }
  }

  public void Dispatch(int id, object item)
  {
    var info = this.methods.Find(i => i.Id == id);

    if (info == null)
    {
      Trace.Error($"unknown method id={id}");
      return;
    }

    if (!this.async && this.queue != null)
    {
      lock (this.queue)
      {
        this.queue.Enqueue(new QueueItem(info, item));

        if (!this.spawnRunning)
          this.spawnRunning = ThreadRunner.Spawn(this.DequeueDispatch);
      }
    }
    else
      ThreadRunner.Spawn(o => this.MethodDispatch(info, item));
  }

  private void DequeueDispatch()
  {
    while (true)
    {
      QueueItem qi;

      lock (this.queue!)
      {
        if (this.queue.Count <= 0)
        {
          this.spawnRunning = false;
          return;
        }

        qi = this.queue.Dequeue();
      }

      this.MethodDispatch(qi.Method, qi.Data);
    }
  }

  private void MethodDispatch(MethodCall info, object data)
  {
    object? answer = null;

    try
    {
      var payload = this.binderInterceptor?.BeforeInvoke(info.Id, data) ?? data;

      if (info.AnswerType == null)
      {
        if (info.Invoker is ProcDelegate proc)
          proc(this.handler, Typer.To<object[]>(payload));
        else if (info.Invoker is PostDelegate post)
          post(this.handler, payload);
      }
      else
      {
        if (info.Invoker is FuncDelegate func)
          answer = func(this.handler, Typer.To<object[]>(payload));
        else if (info.Invoker is SendDelegate send)
          send(this.handler, payload, answer = Activator.CreateInstance(info.AnswerType)!);
      }
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
      answer = ex.ToString();
    }

    try
    {
      this.binderInterceptor?.AfterInvoke(info.Id, data, answer);
    }
    catch (Exception ex)
    {
      Trace.Exception(ex);
    }
  }

  public string GetPacketName(int id)
  {
    var info = this.methods.Find(i => i.Id == id);
    return info?.Info.Name ?? "not found";
  }
}
