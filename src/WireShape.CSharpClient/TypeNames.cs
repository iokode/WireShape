using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IOKode.WireShape.AspNetCore;

namespace IOKode.WireShape.CSharpClient;

/// <summary>
/// What a type is called in the client being written, and which types have to be written for it to mean
/// anything.
/// </summary>
/// <remarks>
/// Nothing is asked of a wire here: the model answered already. Asking for the name of a type is what puts its
/// declaration in the queue, and the queue is empty when everything the calls mention has been written, which
/// is what makes a client carry its own types and no others.
/// </remarks>
public sealed class TypeNames(ApiModel model)
{
    private readonly Dictionary<Type, string> names = new();
    private readonly List<Type> pending = new();
    private readonly HashSet<Type> seen = new();
    private readonly HashSet<Type> reused = new();

    /// <summary>Every type a name has been given to, and the name it was given.</summary>
    public IReadOnlyDictionary<Type, string> Named => names;

    /// <summary>Whether the type is one another client declares, and so is not written here.</summary>
    public bool IsReused(Type type) => reused.Contains(type);

    /// <summary>
    /// A type another client already declares, under the name it declares it by. Asking for it from here answers
    /// that name and never queues a declaration, so the two clients speak of one type and not of two alike.
    /// </summary>
    public void Reuse(Type type, string declaredName)
    {
        names[type] = declaredName;
        seen.Add(type);
        reused.Add(type);
    }

    /// <summary>The types whose declaration has been asked for and not yet written.</summary>
    public IReadOnlyList<Type> TakePending()
    {
        var taken = pending.ToArray();
        pending.Clear();

        return taken;
    }

    /// <summary>
    /// The name a value of this type takes in the client. Asking for it is what puts a type in the queue of what
    /// has to be declared, so a client only carries the types its calls actually mention.
    /// </summary>
    public string Of(Type type, bool isOptional = false)
    {
        var name = Write(type);

        return isOptional && !name.EndsWith("?") ? name + "?" : name;
    }

    private string Write(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            return Write(underlying) + "?";
        }

        // An enumeration keeps its own name whatever it travels as: the values are what it is, and a converter
        // that writes them as text is how they travel, not what they are.
        if (type.IsEnum)
        {
            return Declared(type);
        }

        switch (model.Types.GetValueOrDefault(type))
        {
            case Structure.Carried {NeedsItsOwnName: true}:
                return Declared(type);

            case Structure.Carried carried:
                return Write(carried.By);

            // One of several forms is a type of its own here: C# has no way of saying "a text or an object", so
            // what is written is something that holds either and says which it holds.
            case Structure.OneOf:
                return Declared(type);

            case Structure.File:
                return "ApiFile";
        }

        var primitive = Primitive(type);
        if (primitive != null)
        {
            return primitive;
        }

        var dictionary = AsDictionary(type);
        if (dictionary != null)
        {
            return $"IReadOnlyDictionary<{Write(dictionary.Value.Key)}, {Write(dictionary.Value.Value)}>";
        }

        var item = AsSequence(type);
        if (item != null)
        {
            return $"IReadOnlyList<{Write(item)}>";
        }

        return Declared(type);
    }

    /// <summary>
    /// What a type travels as, when it travels as a bare value and so keeps a name of its own here. Null when it
    /// is written where it is used, there being nothing for a name of ours to add.
    /// </summary>
    public Type? CarriedBy(Type type) =>
        !type.IsEnum && model.Types.GetValueOrDefault(type) is Structure.Carried {NeedsItsOwnName: true} carried
            ? carried.By
            : null;

    private string Declared(Type type)
    {
        if (names.TryGetValue(type, out var known))
        {
            return known;
        }

        var name = Unique(type.Name.Contains('`') ? type.Name[..type.Name.IndexOf('`')] : type.Name);
        names[type] = name;

        if (seen.Add(type))
        {
            pending.Add(type);
        }

        return name;
    }

    private string Unique(string name)
    {
        if (!names.ContainsValue(name))
        {
            return name;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = name + suffix;
            if (!names.ContainsValue(candidate))
            {
                return candidate;
            }
        }
    }

    private static string? Primitive(Type type) => type switch
    {
        _ when type == typeof(string) => "string",
        _ when type == typeof(bool) => "bool",
        _ when type == typeof(byte) => "byte",
        _ when type == typeof(short) => "short",
        _ when type == typeof(int) => "int",
        _ when type == typeof(long) => "long",
        _ when type == typeof(sbyte) => "sbyte",
        _ when type == typeof(ushort) => "ushort",
        _ when type == typeof(uint) => "uint",
        _ when type == typeof(ulong) => "ulong",
        _ when type == typeof(char) => "char",
        _ when type == typeof(float) => "float",
        _ when type == typeof(double) => "double",
        _ when type == typeof(decimal) => "decimal",
        _ when type == typeof(Guid) => "Guid",
        _ when type == typeof(DateTime) => "DateTime",
        _ when type == typeof(DateOnly) => "DateOnly",
        _ when type == typeof(TimeOnly) => "TimeOnly",
        _ when type == typeof(DateTimeOffset) => "DateTimeOffset",
        _ when type == typeof(TimeSpan) => "TimeSpan",
        _ when type == typeof(object) => "System.Text.Json.JsonElement",
        _ => null
    };

    private static (Type Key, Type Value)? AsDictionary(Type type)
    {
        var dictionary = Interfaces(type).FirstOrDefault(candidate =>
            candidate.IsGenericType &&
            (candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
             candidate.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

        return dictionary == null
            ? null
            : (dictionary.GetGenericArguments()[0], dictionary.GetGenericArguments()[1]);
    }

    private static Type? AsSequence(Type type)
    {
        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        return Interfaces(type)
            .FirstOrDefault(candidate =>
                candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static IEnumerable<Type> Interfaces(Type type) =>
        type.IsInterface ? new[] {type}.Concat(type.GetInterfaces()) : type.GetInterfaces();
}
