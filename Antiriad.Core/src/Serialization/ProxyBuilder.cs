using System.Reflection;
using System.Reflection.Emit;
using Antiriad.Core.Helpers;
using Antiriad.Core.IO;
using Antiriad.Core.Log;
using Antiriad.Core.Types;

namespace Antiriad.Core.Serialization;

public class Proxy
{
  public readonly uint Id;
}

public class ProxyBuilder
{
  private static uint counter;

  private static uint GetCounter()
  {
    return Interlocked.Increment(ref counter);
  }

  public static Type BuildProxyType<T>(Type connection) where T : class
  {
    var sourceType = typeof(T);
    var asmName = new AssemblyName($"{sourceType.Name}_Assembly");
    var asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
    var asmModule = asm.DefineDynamicModule($"{sourceType.Name}_Module");
    var parent = typeof(Proxy);
    var dynType = asmModule.DefineType($"{sourceType.Name}_Class", parent.Attributes, parent);

    dynType.AddInterfaceImplementation(sourceType);

    var peerInfo = dynType.DefineField("peer", connection, FieldAttributes.Public);

    BuildConstructor(dynType, connection, peerInfo);
    BuildDispose(dynType, connection, peerInfo);
    BuildMethods(dynType, connection, peerInfo, sourceType);

    return dynType.CreateType();
  }

  private static void BuildConstructor(TypeBuilder dynType, Type peerType, FieldBuilder peerInfo)
  {
    var objcons = Typer.TypeObject.GetConstructor(Array.Empty<Type>());
    var pointCtor = dynType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { peerType });
    var proxyId = typeof(Proxy).GetField("Id");
    var il = pointCtor.GetILGenerator();

    il.Emit(OpCodes.Ldarg_0);                       // load this
    il.Emit(OpCodes.Call, objcons);                 // ancestor constructor

    il.Emit(OpCodes.Ldarg_0);                       // load this
    il.Emit(OpCodes.Ldarg_1);                       // load peerType arg
    il.Emit(OpCodes.Stfld, peerInfo);               // > peerType = peerInfo

    il.Emit(OpCodes.Ldarg_0);                       // load this
    il.Emit(OpCodes.Ldc_I4, GetCounter());          // call GetCounter
    il.Emit(OpCodes.Stfld, proxyId);                // > id = GetCounter()

    il.Emit(OpCodes.Ret);                           // > return
  }

  private static void BuildDispose(TypeBuilder dyntype, Type peerType, FieldBuilder peerInfo)
  {
    var dispMethod = peerType.GetMethod("Deactivate");

    if (dispMethod != null)
    {
      var method = dyntype.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, Typer.TypeVoid, null);
      var il = method.GetILGenerator();

      il.Emit(OpCodes.Ldarg_0);                     // load this
      il.Emit(OpCodes.Ldfld, peerInfo);             // load field peerInfo
      il.Emit(OpCodes.Callvirt, dispMethod);        // > this.peerInfo.Deactivate()
      il.Emit(OpCodes.Ret);                         // > return
    }
  }

  private static void BuildMethods(TypeBuilder dyntype, Type peerType, FieldBuilder peerInfo, Type sourceType)
  {
    var callSend = peerType.GetMethod("SendPacket", new[] { Typer.TypeString, Typer.TypeObject });

    if (callSend == null)
    {
      Trace.Warning($"cannot find method SendPacket for type={peerType.Name}");
    }

    var callPost = peerType.GetMethod("PostPacket", new[] { Typer.TypeString, Typer.TypeObject });

    if (callPost == null)
    {
      Trace.Warning($"cannot find method PostPacket for type={peerType.Name}");
    }

    foreach (var info in sourceType.GetMethods())
    {
      var methodName = MethodBinder.GetMethodName(info);

      Trace.Debug($"dynamic method={info.Name} id={methodName}");

      try
      {
        var isfunc = info.ReturnType != typeof(void);

        if ((isfunc && callSend == null) || (!isfunc && callPost == null))
        {
          Trace.Warning($"ignoring method={methodName}");
          continue;
        }

        var sargs = info.GetParameters().Select(i => i.ParameterType).ToArray();
        var method = dyntype.DefineMethod(info.Name, MethodAttributes.Public | MethodAttributes.Virtual, info.ReturnType, sargs);
        var il = method.GetILGenerator();

        il.DeclareLocal(Typer.TypeObjectArray);                 // declare object array
        il.Emit(OpCodes.Nop);                                   // no-op
        il.Emit(OpCodes.Ldc_I4, sargs.Length);                  // load sargs.Length
        il.Emit(OpCodes.Newarr, Typer.TypeObject);              // creates array
        il.Emit(OpCodes.Stloc_0);                               // > var loc_0 = new object[sargs.Length]

        for (var i = 0; i < sargs.Length; i++)
        {
          il.Emit(OpCodes.Ldloc_0);                             // load loc_0
          il.Emit(OpCodes.Ldc_I4, i);                           // load i
          il.Emit(OpCodes.Ldarg, i + 1);                        // load parameter i
          MethodGenerator.BoxIfNeeded(sargs[i], il);            // box arg[i] if valuetype
          il.Emit(OpCodes.Stelem_Ref);                          // > loc_0[i] = arg[i]
        }

        il.Emit(OpCodes.Ldarg_0);                               // load this
        il.Emit(OpCodes.Ldfld, peerInfo);                       // load field peerInfo
        il.Emit(OpCodes.Ldstr, methodName);                     // load methodName
        il.Emit(OpCodes.Ldloc_0);                               // load loc_0

        if (isfunc)
        {
          il.Emit(OpCodes.Callvirt, callSend);                  // > r = this.SendPacket(methodId, loc_0)
          MethodGenerator.UnboxIfNeeded(method.ReturnType, il); // unbox r if valuetype
        }
        else
          il.Emit(OpCodes.Callvirt, callPost);                  // > this.PostPacket(methodId, loc_0)

        il.Emit(OpCodes.Nop);                                   // no-op
        il.Emit(OpCodes.Ret);                                   // > return
      }
      catch (Exception ex)
      {
        Trace.Error($"Proxy.BindMethods():{ex}");
      }
    }
  }
}
