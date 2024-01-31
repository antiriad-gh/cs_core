namespace Antiriad.Core.Json;

using System;
using System.Collections.Generic;
using System.Threading;
using Antiriad.Core.Threading;
using Antiriad.Core.Log;

/// <summary>
/// Attribute for JSON RPC remote callable methods.
/// Method prototype should be: void(IJsonRpcPacket&lt;T&gt;)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class JsonRpcMethod : Attribute { }

/// <summary>
/// Class Attribute for setting alternative JSON RPC command name.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class JsonRpcCommand : Attribute
{
  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="name">Identifier for command</param>
  public JsonRpcCommand(string name)
  {
    this.Name = name;
  }

  /// <summary>
  /// Command identifier
  /// </summary>
  public string Name { get; private set; }
}

internal delegate void BindableMethod<T>(IJsonRpcPacket<T> packet) where T : class, new();

internal class MethodDelegate
{
  public string Name;
  public Delegate Method;
  public Type ParamType;
}

internal class MethodDispatch
{
  public MethodDispatch(Delegate method, IJsonRpcPacket packet, object context)
  {
    this.Invoker = method;
    this.Packet = packet;
    this.Context = context;
  }

  public IJsonRpcPacket Packet;
  public Delegate Invoker;
  public object Context;
}

internal class MethodBinderPacketInfo
{
  public int Id;
  public object Context;
}

internal class JsonMethodBinder
{
  private readonly List<MethodDelegate> methods = new();
  private readonly Type packetType;
  private readonly bool async;
  private readonly Queue<MethodDispatch> queue;
  private volatile bool spawnRunning;

  public IEnumerable<MethodDelegate> Methods { get { return this.methods; } }
  public ThreadLocal<MethodBinderPacketInfo> PacketInfo { get; private set; }

  public JsonMethodBinder(Type packetType, bool async)
  {
    this.packetType = packetType;
    this.async = async;
    this.PacketInfo = new ThreadLocal<MethodBinderPacketInfo>(() => new MethodBinderPacketInfo());

    if (!async)
      this.queue = new Queue<MethodDispatch>();
  }

  public bool IsBound
  {
    get { return this.methods.Count > 0; }
  }

  private static TResult AGet<TItem, TResult>(IList<TItem> list, int index, Func<TItem, TResult> act, TResult defval = default)
  {
    return list != null && list.Count > index ? act(list[index]) : defval;
  }

  public static string GetMethodName(Type data)
  {
    var attr = data.GetCustomAttributes(typeof(JsonRpcCommand), false);

    if (attr.Length > 0)
      return ((JsonRpcCommand)attr[0]).Name;

    var name = data.Name;
    return name.StartsWith("packet", StringComparison.OrdinalIgnoreCase) ? name[6..] : name;
  }

  /*public void BindMethods<T>(object handler)
  {
    var handlerType = typeof(T);
    this.methods.Clear();

    foreach (var info in handlerType.GetMethods())
    {
      var nameAttr = info.GetCustomAttributes(typeof (JsonRpcCommand), false);
      var name = nameAttr.Length > 0 ? ((JsonRpcCommand) nameAttr[0]).Name : info.Name;
      
      Trace.Debug("bound method={0}->{1}", info.Name, name);

      try
      {
        var delegateType = info.ReturnType == typeof(void) ? typeof(ProcDelegate) : typeof(FuncDelegate);
        var atype = info.ReturnType == typeof(void) ? null : info.ReturnType;
        var mi = MethodGenerator.MakeUntypedDelegateArrayArgument(delegateType, info, this.handler.GetType());
        this.methods.Add(new MethodCall { Id = methodId, Invoker = mi, AnswerType = atype });
      }
      catch (Exception ex)
      {
        Trace.Error($"Proxy.BindMethods():{0}", ex);
      }
    }

    Trace.Debug("Proxy.BindMethods() count={0}", this.methods.Count);
  }*/

  public void BindMethods(object handler)
  {
    this.methods.Clear();

    try
    {
      foreach (var info in handler.GetType().GetMethods())
      {
        if (AGet(info.GetCustomAttributes(typeof(JsonRpcMethod), false), 0, i => i) is JsonRpcMethod attr)
          continue;

        var ptype = AGet(info.GetParameters(), 0, i => i.ParameterType);

        if (ptype == null)
          continue;

        var gtype = AGet(ptype.GetGenericArguments(), 0, i => i);

        if (gtype == null)
          continue;

        var method = JsonMethodBinder.GetMethodName(gtype);
        Trace.Debug($"MethodBinder.BindMethods() id={method}=>{info.Name}");

        var gend = Delegate.CreateDelegate(typeof(BindableMethod<>).MakeGenericType(gtype), handler, info);
        var jtype = this.packetType.MakeGenericType(gtype);
        var item = this.methods.Find(i => i.Name == method);

        if (item == null || info.DeclaringType.IsSubclassOf(item.Method.Method.DeclaringType))
        {
          if (item != null)
          {
            Trace.Debug($"overriding method {item.Method.Method.Name}");
            this.methods.Remove(item);
          }

          this.methods.Add(new MethodDelegate { Name = method, Method = gend, ParamType = jtype });
        }
      }

      Trace.Debug($"MethodBinder.BindMethods() count={this.methods.Count}");
    }
    catch (Exception ex)
    {
      Trace.Error($"MethodBinder.BindMethods():{ex}");
    }
  }

  public void Dispatch(MethodDispatch data)
  {
    if (!this.async)
    {
      lock (this.queue)
      {
        this.queue.Enqueue(data);

        if (!this.spawnRunning)
          this.spawnRunning = ThreadRunner.Spawn(o => this.DequeueDispatch());
      }
    }
    else
      ThreadRunner.Spawn(this.DoDispatch, data);
  }

  private void DequeueDispatch()
  {
    while (true)
    {
      MethodDispatch qi;

      lock (this.queue)
      {
        if (this.queue.Count <= 0)
        {
          this.spawnRunning = false;
          return;
        }

        qi = this.queue.Dequeue();
      }

      this.DoDispatch(qi);
    }
  }

  private void DoDispatch(MethodDispatch md)
  {
    try
    {
      if (md?.Packet == null)
        return;

      if (md.Packet.Id > 0)
        this.PacketInfo.Value.Id = md.Packet.Id;

      if (md.Context != null)
        this.PacketInfo.Value.Context = md.Context;

      md.Invoker.DynamicInvoke(md.Packet);
    }
    catch (Exception ex)
    {
      Trace.Error($"MethodBinder.DoDispatch:{ex}");
    }
  }
}
