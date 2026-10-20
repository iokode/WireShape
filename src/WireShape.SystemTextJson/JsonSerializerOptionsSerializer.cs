using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace IOKode.WireShape.SystemTextJson;

/// <summary>
/// What a wire written with <see cref="JsonSerializerOptions"/> answers about itself.
/// </summary>
/// <remarks>
/// Every answer is read from the options and from nothing else: the members come from the contract they build,
/// and what a value comes out as is read by writing it. One of these per set of options, because the options
/// are the wire.
/// </remarks>
public sealed class JsonSerializerOptionsSerializer(JsonSerializerOptions options) : ISerializer
{
    /// <summary>
    /// The options this wire is written with, for whoever has to ask them something this interface does not
    /// ask: which converter writes a type, and which converters there are. A source written for a wire of JSON
    /// reads them from the writer it is handed, so that it answers about the wire that asked it and not about
    /// one it happened to be built with.
    /// </summary>
    public JsonSerializerOptions Options => options;

    public IReadOnlyList<Member>? GetMembers(Type type)
    {
        JsonTypeInfo contract;
        try
        {
            contract = options.GetTypeInfo(type);
        }
        catch (Exception)
        {
            return null;
        }

        // A contract that describes no object answers no members, which is not the same as answering that there
        // are none: it is what a type a converter of its own took over looks like from here.
        if (contract.Kind == JsonTypeInfoKind.None)
        {
            return null;
        }

        // One for this answer and no longer, because a context of these is not made to be read from two threads
        // and one of these answers whoever asks, from wherever they ask.
        var nullability = new NullabilityInfoContext();

        return contract.Properties
            .Where(property => property.Get != null)
            .Select(property => new Member(
                property.Name,
                property.PropertyType,
                IsOptional(property, nullability),
                IsAnswerOnly: property.Set == null && property.AssociatedParameter == null,
                Declared: property.AttributeProvider as MemberInfo))
            .ToArray();
    }

    public IReadOnlyCollection<Kind>? GetKinds(Type type)
    {
        JsonTypeInfo contract;
        try
        {
            contract = options.GetTypeInfo(type);
        }
        catch (Exception)
        {
            return null;
        }

        if (contract.Kind == JsonTypeInfoKind.None)
        {
            return null;
        }

        if (contract.PolymorphismOptions is not { } hierarchy)
        {
            return Array.Empty<Kind>();
        }

        // Those with a word of their own: a derived type the contract carries without one is written with
        // nothing to tell it apart, so there is no kind to answer with.
        return hierarchy.DerivedTypes
            .Where(derived => derived.TypeDiscriminator != null)
            .Select(derived => new Kind(derived.DerivedType, derived.TypeDiscriminator!.ToString()!))
            .ToArray();
    }

    /// <remarks>
    /// Written and read back out of what came out, because what a value comes out as is decided by a converter
    /// and the only way of knowing what it decided is to let it decide.
    /// </remarks>
    public EnumerationValueForm? GetValueForm(Type enumType, object value)
    {
        var written = JsonSerializer.Serialize(value, enumType, options);

        return written.StartsWith('"')
            ? new EnumerationValueForm.Word(written.Trim('"'))
            : new EnumerationValueForm.Integer(written);
    }

    /// <remarks>
    /// Written and read back, because what turns one into the other is the converter of the wire and there is
    /// no asking it to do half of its work.
    /// </remarks>
    public object? WriteAsCarrier(Type valueType, Type carrier, object? value) =>
        JsonSerializer.Deserialize(JsonSerializer.Serialize(value, valueType, options), carrier, options);

    /// <inheritdoc cref="WriteAsCarrier"/>
    public object? ReadFromCarrier(Type valueType, Type carrier, object? carried) =>
        JsonSerializer.Deserialize(JsonSerializer.Serialize(carried, carrier, options), valueType, options);

    /// <summary>
    /// Whether a member may be absent: a value that may be nothing, or a reference the language says may be
    /// read as null.
    /// </summary>
    private static bool IsOptional(JsonPropertyInfo property, NullabilityInfoContext nullability)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) != null)
        {
            return true;
        }

        return property.AttributeProvider switch
        {
            PropertyInfo declared => nullability.Create(declared).ReadState == NullabilityState.Nullable,
            FieldInfo declared => nullability.Create(declared).ReadState == NullabilityState.Nullable,
            _ => false
        };
    }
}
