using System.Reflection;
using System.Reflection.Emit;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Types;

public delegate object ConstructorHandler();
internal delegate object GetHandler(object source);
internal delegate void SetHandler(object source, object value);

public static class MethodGenerator
{
    internal static ConstructorHandler MakeConstructorHandler(Type type)
    {
        var constructorInfo = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null);

        if (constructorInfo == null)
        {
            if (type.IsAnsiClass)
                return () => Activator.CreateInstance(type)!;

            throw new ApplicationException(string.Format("The type {0} must declare an empty constructor.", type));
        }

        var dynamicMethod = new DynamicMethod("ContructObject", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, Typer.TypeObject, null, type, true);
        var ilgen = dynamicMethod.GetILGenerator();

        ilgen.Emit(OpCodes.Newobj, constructorInfo);
        ilgen.Emit(OpCodes.Ret);

        return (ConstructorHandler)dynamicMethod.CreateDelegate(typeof(ConstructorHandler));
    }

    internal static GetHandler MakeGetHandler(Type type, PropertyInfo propertyInfo)
    {
        if (type.IsAnsiClass && propertyInfo.CanRead)
            return o => propertyInfo.GetValue(o, null)!;

        var getMethodInfo = propertyInfo.GetGetMethod(true);
        var dynamicGet = MakeGetDynamicMethod(type);
        var ilgen = dynamicGet.GetILGenerator();

        ilgen.Emit(OpCodes.Ldarg_0);
        ilgen.Emit(OpCodes.Call, getMethodInfo!);
        BoxIfNeeded(getMethodInfo!.ReturnType, ilgen);
        ilgen.Emit(OpCodes.Ret);

        return (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
    }

    internal static GetHandler MakeGetHandler(Type type, FieldInfo fieldInfo)
    {
        if (type.IsAnsiClass)
            return fieldInfo.GetValue!;

        var dynamicGet = MakeGetDynamicMethod(type);
        var ilgen = dynamicGet.GetILGenerator();

        ilgen.Emit(OpCodes.Ldarg_0);
        ilgen.Emit(OpCodes.Ldfld, fieldInfo);
        BoxIfNeeded(fieldInfo.FieldType, ilgen);
        ilgen.Emit(OpCodes.Ret);

        return (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
    }

    internal static SetHandler? MakeSetHandler(Type type, PropertyInfo propertyInfo)
    {
        try
        {
            if (type.IsAnsiClass && propertyInfo.CanWrite)
                return (o, v) => propertyInfo.SetValue(o, v, null);

            var setMethodInfo = propertyInfo.GetSetMethod(true);

            if (setMethodInfo == null)
                return null;

            var dynamicSet = MakeSetDynamicMethod(type);
            var ilgen = dynamicSet.GetILGenerator();

            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(setMethodInfo.GetParameters()[0].ParameterType, ilgen);
            ilgen.Emit(OpCodes.Call, setMethodInfo);
            ilgen.Emit(OpCodes.Ret);

            return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
        }
        catch
        {
            throw new NotSupportedException(string.Format("Error with type={0}", type));
        }
    }

    internal static SetHandler? MakeSetHandler(Type type, FieldInfo fieldInfo)
    {
        if (type.IsAnsiClass)
            return fieldInfo.SetValue;

        if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
            return null;

        var dynamicSet = MakeSetDynamicMethod(type);
        var ilgen = dynamicSet.GetILGenerator();

        ilgen.Emit(OpCodes.Ldarg_0);
        ilgen.Emit(OpCodes.Ldarg_1);
        UnboxIfNeeded(fieldInfo.FieldType, ilgen);
        ilgen.Emit(OpCodes.Stfld, fieldInfo);
        ilgen.Emit(OpCodes.Ret);

        return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
    }

    internal static DynamicMethod? MakeGenericMethod(MethodInfo? method, Type genericType)
    {
        if (method == null)
            return null;

        var caller = new DynamicMethod("DynamicGeneric_" + genericType.Name, Typer.TypeObject, new[] { Typer.TypeObject }, true);
        var ilgen = caller.GetILGenerator();
        ilgen.Emit(OpCodes.Ldarg_0);
        ilgen.Emit(OpCodes.Call, method.MakeGenericMethod(new[] { genericType }));
        ilgen.Emit(OpCodes.Ret);
        return caller;
    }

    public static T MakeUntypedDelegate<T>(MethodInfo method, object handler)
    {
        return (T)MakeUntypedDelegate(typeof(T), method, handler.GetType());
    }

    public static object MakeUntypedDelegate(Type delegateType, MethodInfo method, Type handlerType)
    {
        var sargs = method.GetParameters().Select(i => i.ParameterType).ToArray();
        var margs = Enumerable.Range(0, sargs.Length + 1).Select(i => typeof(object)).ToArray();
        var caller = new DynamicMethod(method.Name + "_Untyped", method.ReturnType, margs, handlerType, true);
        var ilgen = caller.GetILGenerator();

        ilgen.Emit(OpCodes.Ldarg_0);                    // load this

        for (var i = 0; i < sargs.Length; i++)
        {
            ilgen.Emit(OpCodes.Ldarg_S, i + 1);         // load arg I
            ilgen.Emit(OpCodes.Castclass, sargs[i]);    // cast to arg type
        }

        ilgen.Emit(OpCodes.Call, method);               // call method
        ilgen.Emit(OpCodes.Ret);                        // return

        return caller.CreateDelegate(delegateType);
    }

    // object(object, object[]) / void(object, object[])
    public static object MakeUntypedDelegateArrayArgument(Type delegateType, MethodInfo method, Type handlerType)
    {
        if (method == null)
            throw new Exception("method info cannot be null");

        var isfunc = method.ReturnType != typeof(void);
        var sargs = method.GetParameters();
        var margs = new[] { Typer.TypeObject, Typer.TypeObjectArray };
        var caller = new DynamicMethod(method.Name + "_UntypedA", isfunc ? typeof(object) : typeof(void), margs, handlerType, true);
        var il = caller.GetILGenerator();
        var locals = new LocalBuilder[sargs.Length];

        if (!method.IsStatic)
            il.Emit(OpCodes.Ldarg_0);                // load this

        for (var i = 0; i < sargs.Length; i++)
        {
            if (sargs[i].IsOut || sargs[i].ParameterType.IsByRef)
            {
                locals[i] = il.DeclareLocal(sargs[i].ParameterType);
                il.Emit(OpCodes.Ldloca, locals[i]);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_1);            // load array
                il.Emit(OpCodes.Ldc_I4, i);          // load array index
                il.Emit(OpCodes.Ldelem_Ref);         // load array[index]
            }

            UnboxIfNeeded(sargs[i].ParameterType, il);         // unbox based on element type
        }

        if (method.IsFinal || !method.IsVirtual) // call method
        {
            il.Emit(OpCodes.Call, method);
        }
        else
        {
            il.Emit(OpCodes.Callvirt, method);
        }

        if (isfunc)
            BoxIfNeeded(method.ReturnType, il);  // box if needed

        for (int i = 0; i < sargs.Length; ++i)
        {
            if (sargs[i].IsOut || sargs[i].ParameterType.IsByRef)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldloc, locals[i].LocalIndex);

                BoxIfNeeded(sargs[i].ParameterType.GetElementType()!, il);

                il.Emit(OpCodes.Stelem_Ref);
            }
        }

        il.Emit(OpCodes.Ret);                    // return

        return caller.CreateDelegate(delegateType);
    }

    internal static T? MakeGenericDelegate<T>(MethodInfo? method, Type genericType)
    {
        return (T?)(object?)MethodGenerator.MakeGenericMethod(method, genericType)?.CreateDelegate(typeof(T));
    }

    private static DynamicMethod MakeGetDynamicMethod(Type type)
    {
        return new DynamicMethod("DynamicGet", Typer.TypeObject, new[] { Typer.TypeObject }, type, true);
    }

    private static DynamicMethod MakeSetDynamicMethod(Type type)
    {
        return new DynamicMethod("DynamicSet", Typer.TypeVoid, new[] { Typer.TypeObject, Typer.TypeObject }, type, true);
    }

    public static void BoxIfNeeded(Type type, ILGenerator generator)
    {
        if (type.IsValueType)
            generator.Emit(OpCodes.Box, type);
    }

    public static void UnboxIfNeeded(Type type, ILGenerator generator)
    {
        if (type.IsValueType)
            generator.Emit(OpCodes.Unbox_Any, type);
    }
}
