using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Antiriad.Core.Helpers;

namespace Antiriad.Core.Types;

public class CachedTypeInfo
{
    public readonly Type Type;
    public readonly string Name;
    public readonly Dictionary<string, PropertyMetadata> Props;
    public readonly ConstructorHandler Constructor;

    public CachedTypeInfo(Type type, BindingFlags? flags = null)
    {
        this.Type = type;
        this.Name = GetCleanName(this.Type.AssemblyQualifiedName);

        var metap = new List<PropertyMetadata>();
        var finalFlags = flags ?? BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;
        var ignoreAttributes = new[] { typeof(CompilerGeneratedAttribute), typeof(IgnoreDataMemberAttribute), typeof(NonSerializedAttribute) };

        try
        {
            var list = type.GetFields(finalFlags);
            metap.AddRange(list.Where(i => !i.GetCustomAttributes(true).Select(i => i.GetType()).Intersect(ignoreAttributes).Any()).OrderBy(i => i.Name).Select(i => new PropertyMetadata(this.Type, i)));
        }
        catch (Exception)
        {
        }

        try
        {
            var list = type.GetProperties(finalFlags);
            metap.AddRange(list.Where(i => !i.GetCustomAttributes(true).Select(i => i.GetType()).Intersect(ignoreAttributes).Any() && i.CanWrite).OrderBy(i => i.Name).Select(i => new PropertyMetadata(this.Type, i)));
        }
        catch (Exception)
        {
        }

        if (type.IsArray || type.IsPrimitive || type == Typer.TypeString)
        {
            this.Constructor = () => Activator.CreateInstance(type)!;
        }
        else if (type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null)
        {
            this.Constructor = MethodGenerator.MakeConstructorHandler(type);
        }
        else
        {
            this.Constructor = () => FormatterServices.GetUninitializedObject(type);
        }

        this.Props = metap.ToDictionary(i => i.Name, i => i);
    }

    public static string GetCleanName(string? name)
    {
        if (name == null)
            throw new Exception($"cannot get assembly name from {name}");

        var xx = name.AsSpan();

        ReadOnlySpan<char> src = name.AsSpan();
        Span<char> dst = stackalloc char[src.Length];
        var ignore = false;
        var comma = false;
        var index = 0;

        for (var i = 0; i < src.Length; i++)
        {
            var c = src[i];

            if (c == '[')
            {
                comma = true;
            }
            else if (c == ']')
            {
                comma = false;
                ignore = false;
            }
            else if (comma && c == ',')
            {
                ignore = true;
            }
            else if (c == ',')
            {
                comma = true;
            }

            if (!ignore)
            {
                dst[index++] = c;
            }
        }

        return dst[..index].ToString();
    }

    /*public static string GetCleanAssemblyName(string? name)
    {
        if (name == null)
            throw new Exception($"cannot get assembly name from {name}");

        var index1 = name.IndexOf(',', 0);
        var index2 = name.IndexOf(',', index1 + 1);
        return index2 >= 0 ? name[..index2] : name;
    }*/

    public object? NewInstance()
    {
        try
        {
            return this.Constructor();
        }
        catch (Exception)
        {
        }

        return null;
    }
}
